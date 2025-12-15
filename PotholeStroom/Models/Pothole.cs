namespace PotholeStroom.Models
{
    public class Pothole
    {
        public string Id { get; set; }
        public string WorkerId { get; set; }
        public string Location { get; set; }
        public double Cost { get; set; }
        public string Timestamp { get; set; }
        public double AmountDonated { get; set; }
        public string Status { get; set; } = "Unfinanced";

        public double RemainingCost => Cost - AmountDonated;
        public bool WorkerNotification { get; set; } = false;


    }
}
