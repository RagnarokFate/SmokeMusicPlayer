namespace SmokeMusicPlayer.Data
{
    public struct AudioTrackMetadata
    {
        public string filePath;
        public string fileName;
        public float duration;
        public int sampleRate;
        public int channels;

        public AudioTrackMetadata(string filePath, string fileName, float duration, int sampleRate, int channels)
        {
            this.filePath = filePath;
            this.fileName = fileName;
            this.duration = duration;
            this.sampleRate = sampleRate;
            this.channels = channels;
        }
    }
}
