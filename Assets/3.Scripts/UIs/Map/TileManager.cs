public class TileData
{
    private CharacterBase _character;

    public CharacterBase Character
    {
        get => _character;

        set
        {
            _character = value;
            isempty = (_character == null);
        }
    }

    public bool ismovable = false;
    public bool isempty = true;
    public bool isvariable = false;

    public enum tiletype
    {
        inside,
        outside
    }
}