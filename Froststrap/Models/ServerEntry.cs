namespace Froststrap.Models
{
    public class ServerEntry
    {
        public int Number { get; set; }
        public string ServerId { get; set; } = null!;
        public string Players { get; set; } = null!;
        public string Region { get; set; } = null!;
        public int? DataCenterId { get; set; }
        public string Uptime { get; set; } = "Loading...";
        public int PingMs { get; set; }
        public bool PingIsMeasured { get; set; }
        public string? IpAddress { get; set; }
        public string PingText =>
            PingMs <= 0 ? "—" : (PingIsMeasured ? $"{PingMs} ms" : $"~{PingMs} ms");
        public System.Windows.Input.ICommand? JoinCommand { get; set; }
    }
}
