namespace FlightBooking.DTOs
{
    public class SeatMapDto
    {
        public int FlightId { get; set; }
        public string FlightNumber { get; set; }
        public string AircraftModel { get; set; }
        public string SeatMapLayout { get; set; }
        public List<SeatDto> Seats { get; set; } = new List<SeatDto>();
    }

    public class SeatDto
    {
        public int SeatId { get; set; }
        public string SeatNumber { get; set; }
        public int SeatRow { get; set; }
        public string SeatColumn { get; set; }
        public string SeatClassName { get; set; }
        public bool IsWindow { get; set; }
        public bool IsAisle { get; set; }
        public bool IsEmergencyExit { get; set; }
        public decimal ExtraFee { get; set; }
        public bool IsAvailable { get; set; }
        public decimal TotalPrice { get; set; }
        public bool IsBookedByCurrentUser { get; set; }
    }
}
