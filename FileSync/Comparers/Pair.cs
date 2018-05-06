namespace FileSync.Comparers
{
    public class Pair
    {
        public Pair(string sourcePath, string destinationPath)
        {
            SourcePath = sourcePath;
            DestinationPath = destinationPath;
        }

        public string SourcePath { get; private set; }

        public string DestinationPath { get; private set; }

        public bool HasSynced { get; private set; }

        public void Done()
        {
            HasSynced = true;
        }
    }
}