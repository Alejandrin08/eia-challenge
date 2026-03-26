namespace Eia.Data.Entities
{
    public class ExtractionRun
    {
        public int Id { get; set; }

        public DateTime ExtractedAt { get; set; }

        public int RecordCount { get; set; }

        public string Status { get; set; } = string.Empty;

        public string? ErrorMessage { get; set; }

        public ICollection<NuclearOutage> NuclearOutages { get; set; } = [];
    }
}