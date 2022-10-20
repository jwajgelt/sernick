using sernick.Input;

namespace sernick.Utility;

public static class FileUtility
{
    public static IInput ReadFile(this string fileName)
    {
        return new FileInput(fileName);
    }

    private sealed class FileInput : IInput
    {
        internal FileInput(string fileName)
        {
            _lines = File.ReadAllLines(fileName).Select(line => line + '\n').ToArray();
            Start = new FileLocation(0, 0);
            End = new FileLocation(_lines.Length, 0);
            CurrentLocation = Start;
            Current = _lines[0][0];
        }

        private readonly string[] _lines;

        public bool MoveNext()
        {
            if (ReferenceEquals(CurrentLocation, End))
            {
                return false;
            }

            var line = _lines[((FileLocation)CurrentLocation).Line];

            // if at the end of current line then move to the next line
            if (line.Length <= ((FileLocation)CurrentLocation).Character + 1)
            {
                // if below the last line then move to End
                if (_lines.Length <= ((FileLocation)CurrentLocation).Line + 1)
                {
                    CurrentLocation = End;
                    return false;
                }

                // moving to the beginning of the next line
                CurrentLocation = new FileLocation(((FileLocation)CurrentLocation).Line + 1, 0);
                return true;
            }

            // if moving to the next character
            CurrentLocation = new FileLocation(((FileLocation)CurrentLocation).Line, ((FileLocation)CurrentLocation).Character + 1);
            return true;
        }

        public void MoveTo(ILocation location)
        {
            // if location is not FileLocation type then skip this call
            if (location is not FileLocation fileLocation)
            {
                return;
            }

            var line = fileLocation.Line;
            var character = fileLocation.Character;

            // if location is invalid then skip this call
            if (_lines.Length <= line || _lines[line].Length <= character)
            {
                return;
            }

            CurrentLocation = location;
            Current = _lines[line][character];
        }

        public char Current { get; private set; }
        public ILocation CurrentLocation { get; private set; }
        public ILocation Start { get; }
        public ILocation End { get; }

        private sealed class FileLocation : ILocation
        {
            internal FileLocation(int line, int character)
            {
                Line = line;
                Character = character;
            }

            internal readonly int Line;
            internal readonly int Character;

            public new string ToString()
            {
                return $"line {Line}, character {Character}";
            }
        }
    }
}
