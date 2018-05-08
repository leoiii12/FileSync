namespace FileSync.Comparers
{
    public class Pair
    {
        public Pair(string sourcePath, string destinationPath)
        {
            SourcePath = sourcePath;
            DestinationPath = destinationPath;
        }

        public string SourcePath { get; }

        public string DestinationPath { get; }

        public bool HasSynced { get; private set; }

        public void Done()
        {
            HasSynced = true;
        }
    }
}