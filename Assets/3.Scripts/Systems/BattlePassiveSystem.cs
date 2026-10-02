using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Battle-owned runtime state. PassiveSkill assets contain configuration only.
/// Passive damage never emits another attack/movement event, preventing support-attack loops.
/// </summary>
public sealed class BattlePassiveSystem
{
    private sealed class Burn
    {
        public CharacterBase source;
        public int damage;
        public int turns;
    }

    private sealed class Bomb
    {
        public CharacterBase source;
        public PassiveSkill skill;
        public Tilemap map;
        public Vector3Int cell;
        public int damage;
        public int radius;
        public int turns;
    }

    private readonly Dictionary<CharacterBase, Dictionary<PassiveSkill, int>> activations
        = new Dictionary<CharacterBase, Dictionary<PassiveSkill, int>>();
    private readonly Dictionary<CharacterBase, Burn> burns = new Dictionary<CharacterBase, Burn>();
    private readonly Dictionary<CharacterBase, int> stuns = new Dictionary<CharacterBase, int>();
    private readonly HashSet<CharacterBase> stunnedThisTurn = new HashSet<CharacterBase>();
    private readonly List<Bomb> bombs = new List<Bomb>();
    private readonly Dictionary<CharacterBase, int> pendingMovement = new Dictionary<CharacterBase, int>();
    private bool processingMonsterTurn;
    private int playerTurn = -1;
    private int lastStatusTurn = -1;

    public bool IsResolving { get; private set; }
    public int BombCount => bombs.Count;
    public event Action<CharacterBase, PassiveSkill> PassiveActivated;

    public void ResetBattle()
    {
        activations.Clear();
        burns.Clear();
        stuns.Clear();
        stunnedThisTurn.Clear();
        bombs.Clear();
        pendingMovement.Clear();
        processingMonsterTurn = false;
        playerTurn = -1;
        lastStatusTurn = -1;
        IsResolving = false;
    }

    public void BeginPlayerTurn(int turn)
    {
        if (playerTurn == turn) return;
        playerTurn = turn;
        activations.Clear();
    }

    // DOT kills happen before the normal stamina reset; carry their rewards into the new turn.
    public void ApplyStartOfTurnBonuses(CharacterBase player)
    {
        int amount;
        if (IsPlayer(player) && pendingMovement.TryGetValue(player, out amount))
        {
            player.steminaPoint += amount;
            pendingMovement.Remove(player);
        }
    }

    // Players use shared battle HP; their individual HP can legitimately be zero.
    public static bool IsPlayer(CharacterBase character)
    {
        return character != null && character.gameObject.activeInHierarchy
            && character.isSpawned && !character.isEnemy && !(character is MonsterBase);
    }

    private static bool IsEnemy(CharacterBase character)
    {
        return character != null && character.gameObject.activeInHierarchy
            && (character.isEnemy || character is MonsterBase) && !character.IsDead;
    }

    public static List<PassiveSkill> EquippedPassives(CharacterBase character)
    {
        var result = new List<PassiveSkill>();
        if (character == null || character.Data == null) return result;
        AddUnique(result, character.Data.staticpassive);
        if (character.Data.passive != null)
            foreach (PassiveSkill skill in character.Data.passive) AddUnique(result, skill);
        return result;
    }

    private static void AddUnique(List<PassiveSkill> skills, PassiveSkill skill)
    {
        if (skill != null && skill.passiveEffect != PassiveEffect.None && !skills.Contains(skill))
            skills.Add(skill);
    }

    public void OnAttackCompleted(CharacterBase actor, SkillList attack, SkillExecuteResult result,
        Vector3Int aim, ICollection<Vector3Int> attackArea, IList<CharacterBase> characters, Tilemap map)
    {
        if (IsResolving || !IsPlayer(actor) || attack == null || result == null || map == null
            || !attack.effectType.HasFlag(SkillEffectType.Damage) || attack.type == SkillType.Passive)
            return;

        Resolve(actor, aim, attackArea, result, false, characters, map);
    }

    public void OnMovementCompleted(CharacterBase actor, Vector3Int from, Vector3Int to,
        IList<CharacterBase> characters, Tilemap map)
    {
        if (IsResolving || !IsPlayer(actor) || from == to || map == null || !map.HasTile(to)) return;
        Resolve(actor, to, null, null, true, characters, map);
    }

    private void Resolve(CharacterBase actor, Vector3Int aim, ICollection<Vector3Int> attackArea,
        SkillExecuteResult result, bool movement, IList<CharacterBase> characters, Tilemap map)
    {
        if (characters == null) return;
        IsResolving = true;
        try
        {
            map.CompressBounds();
            BoundsInt bounds = map.cellBounds;
            Vector3Int actorCell = Cell(actor, map);
            // Freeze the event's eligible owners before damage can remove scene objects.
            var owners = new List<CharacterBase>(characters);
            foreach (CharacterBase owner in owners)
            {
                if (!IsPlayer(owner) || !map.HasTile(Cell(owner, map))) continue;
                foreach (PassiveSkill skill in EquippedPassives(owner))
                {
                    if (!Matches(skill.trigger, movement, owner, actor, actorCell, map, bounds)
                        || !CanActivate(owner, skill)) continue;

                    if (Apply(owner, actor, skill, aim, attackArea, result, characters, map, bounds))
                        Activated(owner, skill);
                }
            }
        }
        finally { IsResolving = false; }
    }

    private static bool Matches(PassiveTrigger trigger, bool movement, CharacterBase owner,
        CharacterBase actor, Vector3Int actorCell, Tilemap map, BoundsInt bounds)
    {
        if (movement)
            return trigger == PassiveTrigger.AdjacentAllyMoved && owner != actor
                && PassiveGeometry.AreSideNeighbours(Cell(owner, map), actorCell, bounds);

        switch (trigger)
        {
            case PassiveTrigger.OwnerAttackCompleted: return owner == actor;
            case PassiveTrigger.AdjacentAllyAttackCompleted:
                return owner != actor && PassiveGeometry.AreSideNeighbours(Cell(owner, map), actorCell, bounds);
            case PassiveTrigger.OppositeAllyAttackCompleted:
                return owner != actor && PassiveGeometry.AreOpposite(Cell(owner, map), actorCell, bounds);
            default: return false;
        }
    }

    private bool Apply(CharacterBase owner, CharacterBase actor, PassiveSkill skill,
        Vector3Int aim, ICollection<Vector3Int> attackArea, SkillExecuteResult result,
        IList<CharacterBase> characters, Tilemap map, BoundsInt bounds)
    {
        CharacterBase target;
        switch (skill.passiveEffect)
        {
            case PassiveEffect.AttackFront:
                Vector3Int front = PassiveGeometry.FrontCell(Cell(owner, map), aim, bounds);
                if (!map.HasTile(front)) return false;
                target = FindEnemyAt(front, characters, map);
                return Damage(owner, target, skill.passiveDamage);
            case PassiveEffect.PlantBomb:
                if (!map.HasTile(aim) || skill.passiveDamage <= 0) return false;
                Bomb bomb = bombs.Find(item => item.source == owner && item.skill == skill
                    && item.map == map && item.cell == aim);
                if (bomb == null)
                {
                    bomb = new Bomb { source = owner, skill = skill, map = map, cell = aim };
                    bombs.Add(bomb);
                }
                bomb.damage = skill.passiveDamage;
                bomb.radius = Mathf.Max(0, skill.bombRadius);
                bomb.turns = Mathf.Max(1, skill.durationTurns);
                return true;
            case PassiveEffect.AttackNearestToActor:
                return Damage(owner, Nearest(Cell(actor, map), characters, map), skill.passiveDamage);
            case PassiveEffect.AttackNearestToOwner:
                return Damage(owner, Nearest(Cell(owner, map), characters, map), skill.passiveDamage);
            case PassiveEffect.RestoreActorMovement:
                return RestoreMovement(actor, skill.movementPoints);
            case PassiveEffect.RestoreMovementPerKill:
                return result != null && RestoreMovement(owner, result.killCount * skill.movementPoints);
            case PassiveEffect.ExtraActionAtCorner:
                if (!PassiveGeometry.IsCorner(Cell(owner, map), bounds)) return false;
                owner.actionPoint += Mathf.Max(1, skill.extraActions);
                owner.UpdateActionStateVisual();
                return true;
            case PassiveEffect.ApplyRandomBurn:
                target = RandomEnemy(characters, map);
                if (target == null || skill.passiveDamage <= 0) return false;
                Burn burn;
                if (!burns.TryGetValue(target, out burn))
                {
                    burn = new Burn();
                    burns.Add(target, burn);
                }
                burn.source = owner;
                burn.damage = Mathf.Max(burn.damage, skill.passiveDamage);
                burn.turns = Mathf.Max(burn.turns, Mathf.Max(1, skill.durationTurns));
                return true;
            case PassiveEffect.ApplyRandomStun:
                target = RandomEnemy(characters, map);
                if (target == null) return false;
                int remaining;
                stuns.TryGetValue(target, out remaining);
                stuns[target] = Mathf.Max(remaining, Mathf.Max(1, skill.durationTurns));
                return true;
            case PassiveEffect.AttackSurvivors:
                if (result == null || attackArea == null) return false;
                bool hitAny = false;
                foreach (CharacterBase survivor in result.damagedTargets)
                {
                    // Do not hit enemies pushed out of the original AOE, or already killed by support.
                    if (IsEnemy(survivor) && attackArea.Contains(Cell(survivor, map)))
                        hitAny |= Damage(owner, survivor, skill.passiveDamage);
                }
                return hitAny;
            default: return false;
        }
    }

    private bool CanActivate(CharacterBase owner, PassiveSkill skill)
    {
        if (skill.maxActivationsPerTurn <= 0) return true;
        Dictionary<PassiveSkill, int> counts;
        int count;
        return !activations.TryGetValue(owner, out counts) || !counts.TryGetValue(skill, out count)
            || count < skill.maxActivationsPerTurn;
    }

    private void Activated(CharacterBase owner, PassiveSkill skill)
    {
        Dictionary<PassiveSkill, int> counts;
        if (!activations.TryGetValue(owner, out counts))
        {
            counts = new Dictionary<PassiveSkill, int>();
            activations.Add(owner, counts);
        }
        int count;
        counts.TryGetValue(skill, out count);
        counts[skill] = count + 1;
        PassiveActivated?.Invoke(owner, skill);
    }

    private bool Damage(CharacterBase source, CharacterBase target, int damage)
    {
        if (!IsEnemy(target) || damage <= 0) return false;
        target.TakeDamage(damage);
        // Kill rewards may run, but attack passives cannot recursively trigger more attacks.
        if (target == null || target.IsDead || !target.gameObject.activeInHierarchy)
        {
            if (IsPlayer(source))
                foreach (PassiveSkill skill in EquippedPassives(source))
                    if (skill.passiveEffect == PassiveEffect.RestoreMovementPerKill
                        && CanActivate(source, skill) && RestoreMovement(source, skill.movementPoints))
                        Activated(source, skill);
        }
        return true;
    }

    private bool RestoreMovement(CharacterBase character, int amount)
    {
        if (!IsPlayer(character) || amount <= 0) return false;
        // A bonus can exceed the normal per-turn allowance and is reset on the next player turn.
        if (processingMonsterTurn)
        {
            int pending;
            pendingMovement.TryGetValue(character, out pending);
            pendingMovement[character] = pending + amount;
        }
        else character.steminaPoint += amount;
        return true;
    }

    public int BurnTurns(CharacterBase target)
    {
        Burn burn;
        return target != null && burns.TryGetValue(target, out burn) ? burn.turns : 0;
    }

    public int StunTurns(CharacterBase target)
    {
        int turns;
        return target != null && stuns.TryGetValue(target, out turns) ? turns : 0;
    }

    public bool ShouldSkipMonsterAction(CharacterBase target) => stunnedThisTurn.Contains(target);

    /// <summary>Called once before monsters subtract their remaining HP from shared battle HP.</summary>
    public void BeforeMonsterTurn(int turn, IList<CharacterBase> characters, Tilemap map)
    {
        if (IsResolving || lastStatusTurn == turn || characters == null || map == null) return;
        lastStatusTurn = turn;
        IsResolving = true;
        processingMonsterTurn = true;
        try
        {
            foreach (CharacterBase target in new List<CharacterBase>(burns.Keys))
            {
                Burn burn = burns[target];
                if (!IsEnemy(target)) { burns.Remove(target); continue; }
                Damage(burn.source, target, burn.damage);
                burn.turns--;
                if (!IsEnemy(target) || burn.turns <= 0) burns.Remove(target);
            }

            for (int i = bombs.Count - 1; i >= 0; i--)
            {
                Bomb bomb = bombs[i];
                if (bomb.map != map || !map.HasTile(bomb.cell)) { bombs.RemoveAt(i); continue; }
                foreach (CharacterBase target in characters)
                    if (IsEnemy(target) && PassiveGeometry.Distance(Cell(target, map), bomb.cell) <= bomb.radius)
                        Damage(bomb.source, target, bomb.damage);
                bomb.turns--;
                if (bomb.turns <= 0) bombs.RemoveAt(i);
            }

            stunnedThisTurn.Clear();
            foreach (CharacterBase target in new List<CharacterBase>(stuns.Keys))
            {
                if (!IsEnemy(target)) { stuns.Remove(target); continue; }
                stunnedThisTurn.Add(target);
                int turns = stuns[target] - 1;
                if (turns <= 0) stuns.Remove(target);
                else stuns[target] = turns;
            }
        }
        finally { processingMonsterTurn = false; IsResolving = false; }
    }

    private static Vector3Int Cell(CharacterBase character, Tilemap map)
    {
        Vector3Int cell = map.WorldToCell(character.transform.position);
        cell.z = 0;
        return cell;
    }

    private static CharacterBase FindEnemyAt(Vector3Int cell, IList<CharacterBase> characters, Tilemap map)
    {
        foreach (CharacterBase target in characters)
            if (IsEnemy(target) && Cell(target, map) == cell) return target;
        return null;
    }

    private static CharacterBase Nearest(Vector3Int from, IList<CharacterBase> characters, Tilemap map)
    {
        CharacterBase nearest = null;
        int bestDistance = int.MaxValue;
        foreach (CharacterBase target in characters)
        {
            if (!IsEnemy(target) || !map.HasTile(Cell(target, map))) continue;
            int distance = PassiveGeometry.Distance(from, Cell(target, map));
            if (distance < bestDistance || (distance == bestDistance
                && nearest != null && target.GetInstanceID() < nearest.GetInstanceID()))
            {
                bestDistance = distance;
                nearest = target;
            }
        }
        return nearest;
    }

    private static CharacterBase RandomEnemy(IList<CharacterBase> characters, Tilemap map)
    {
        var enemies = new List<CharacterBase>();
        foreach (CharacterBase target in characters)
            if (IsEnemy(target) && map.HasTile(Cell(target, map))) enemies.Add(target);
        return enemies.Count == 0 ? null : enemies[UnityEngine.Random.Range(0, enemies.Count)];
    }
}
