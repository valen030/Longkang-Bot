namespace LKGServiceBot.Helper
{
    public class ConstMessage
    {
        public const string USER_NOT_IN_VOICE_CHANNEL = "You must be connected to a voice channel to execute command!";

        public const string JOINED_VOICE_CHANNEL = "I've joined channel '{0}'.";
        public const string LEFT_VOICE_CHANNEL = "I've left the voice channel.";

        public const string TRACK_NOT_FOUND = "Track not found.";
        public const string TRACK_ADDED_TO_QUEUE = "Track '{0}' added to the queue.";
        public const string TRACK_PAUSED = "Track paused : '{0}'";
        public const string TRACK_RESUMED = "Track is resumed.";
        public const string TRACK_STOPPED = "Track stopped.";
        public const string TRACK_PLAYING = "Now playing: {0}";
        public const string TRACK_EMPTY = "No Track is playing.";

        public const string TRACK_LOOP_ENABLED = "Loop has been enabled.";
        public const string TRACK_LOOP_DISABLED = "Loop has been disabled.";

        public const string QUEUE_EMPTY = "The queue is empty.";
        public const string QUEUE_TOTAL = "Total of {0} tracks added to the queue.";
        public const string QUEUE_CLEARED = "The queue has been cleared.";
        public const string QUEUE_SHUFFLED = "The queue has been shuffled.";
        public const string QUEUE_LIST = "Current Queue:\n{0}";
        public const string QUEUE_TRACK_NOT_FOUND = "The track number '{0}' does not exist in the queue.";
        public const string QUEUE_TRACK_REMOVED = "The track {0} has been removed from the queue.";

        public const string NOTHING_ACTION= "Nothing to {0}.";
        public const string INVALID_NUMBER= "Please provide a valid number.";
        public const string INVALID_SEARCH = "Please provide search terms.";
    }
}
