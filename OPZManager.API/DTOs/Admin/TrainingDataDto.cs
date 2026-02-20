namespace OPZManager.API.DTOs.Admin
{
    public class TrainingDataDto
    {
        public int Id { get; set; }
        public string Question { get; set; } = string.Empty;
        public string Answer { get; set; } = string.Empty;
        public string Context { get; set; } = string.Empty;
        public string DataType { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class CreateTrainingDataDto
    {
        public string Question { get; set; } = string.Empty;
        public string Answer { get; set; } = string.Empty;
        public string Context { get; set; } = string.Empty;
        public string DataType { get; set; } = "QA";
    }
}
