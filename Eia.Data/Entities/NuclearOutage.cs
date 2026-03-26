namespace Eia.Data.Entities
{
    public class NuclearOutage
    {
        public int Id { get; set; }

        public string Period { get; set; } = string.Empty;

        public double? CapacityMw { get; set; }

        public double? OutageMw { get; set; }

        public double? PercentOutage { get; set; }

        public int ExtractionRunId { get; set; }

        public ExtractionRun ExtractionRun { get; set; } = null!;
    }
}