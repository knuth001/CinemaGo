namespace CinemaGo;

public enum UserRole { Customer, Admin }
public enum BookingStatus { Reserved, Confirmed, Cancelled }
public enum SessionStatus { Active, Cancelled }
public enum TicketType { Standard, VIP }

public abstract class User
{
    public int Id { get; set; }
    public string Username { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string Email { get; set; } = "";
    public UserRole Role { get; set; }
    public virtual void DisplayMenu(CinemaSystem app) => app.CustomerMenu((Customer)this);
}
public sealed class Customer : User { }
public sealed class Admin : User
{
    public override void DisplayMenu(CinemaSystem app) => app.AdminMenu(this);
}
public sealed class Movie
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string Genre { get; set; } = "";
    public int DurationMinutes { get; set; }
    public string AgeRating { get; set; } = "";
}
public sealed class Session
{
    public string Id { get; set; } = "";
    public string MovieId { get; set; } = "";
    public string HallId { get; set; } = "";
    public DateTime StartDateTime { get; set; }
    public decimal StandardPrice { get; set; }
    public decimal VipPrice { get; set; }
    public SessionStatus Status { get; set; } = SessionStatus.Active;
}
public sealed class Hall
{
    public string Id { get; set; } = "H1";
    public int Rows { get; set; } = 6;
    public int SeatsPerRow { get; set; } = 10;
    public HashSet<string> VipSeats { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
public sealed class Seat
{
    public int Row { get; }
    public int Number { get; }
    public bool IsVip { get; }
    private bool _booked;
    public bool IsBooked => _booked;
    public Seat(int row, int number, bool vip) { Row=row; Number=number; IsVip=vip; }
    public bool Book() { if (_booked) return false; _booked=true; return true; }
    public void Cancel() => _booked=false;
}
public abstract class Ticket
{
    public string Code { get; }
    public decimal Price { get; }
    protected Ticket(string code, decimal price) { Code=code; Price=price; }
    public abstract decimal CalculatePrice(decimal basePrice, decimal discountPercent=0);
    public virtual void PrintTicket(Session session, Movie movie, int row, int seat)
        => Console.WriteLine($"Ticket {Code} | {movie.Title} | {session.StartDateTime:yyyy-MM-dd HH:mm} | Hall {session.HallId} | Row {row}, Seat {seat} | {Price:0.00} ₾");
}
public sealed class StandardTicket : Ticket
{
    public StandardTicket(string code, decimal price):base(code,price){}
    public override decimal CalculatePrice(decimal basePrice, decimal discountPercent=0)
        => Math.Max(0, basePrice * (1-Math.Clamp(discountPercent,0,100)/100m));
}
public sealed class VipTicket : Ticket
{
    public VipTicket(string code, decimal price):base(code,price){}
    public override decimal CalculatePrice(decimal basePrice, decimal discountPercent=0)
        => Math.Max(0, basePrice * (1-Math.Clamp(discountPercent,0,100)/100m));
}
public sealed class Booking
{
    public string Id { get; set; } = "";
    public int UserId { get; set; }
    public string SessionId { get; set; } = "";
    public int Row { get; set; }
    public int Seat { get; set; }
    public TicketType TicketType { get; set; }
    public decimal Price { get; set; }
    public string TicketCode { get; set; } = "";
    public BookingStatus Status { get; set; } = BookingStatus.Confirmed;
}
