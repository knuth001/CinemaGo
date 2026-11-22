using System.Globalization;

namespace CinemaGo;

public sealed class CinemaSystem
{
    private readonly FileManager _files = new();
    private readonly Logger _log;
    private readonly EmailService _email;
    private readonly List<User> _users=new();
    private readonly List<Movie> _movies=new();
    private readonly List<Session> _sessions=new();
    private readonly List<Booking> _bookings=new();
    private readonly Dictionary<string,Hall> _halls=new();
    private User? _current;
    private const string DateFormat="yyyy-MM-dd HH:mm";

    public CinemaSystem()
    {
        _log=new Logger(_files.PathFor(""));
        _email=new EmailService(_log);
        Seed();
        Load();
    }
    public void Run()
    {
        while(true)
        {
            Console.Clear(); Console.WriteLine("=== CinemaGo ===\n1. Login\n2. Register\n3. Browse movies/sessions\n0. Exit");
            switch(ReadInt("Choose: ",0,3))
            {
                case 1: Login(); break;
                case 2: Register(); break;
                case 3: BrowseSessions(); Pause(); break;
                case 0: return;
            }
        }
    }
    private void Seed()
    {
        _halls["H1"]=new Hall{Id="H1",Rows=6,SeatsPerRow=10,VipSeats=new HashSet<string>{"1-5","1-6","2-5","2-6"}};
        if(_files.Read("Users.txt").Count==0)
            _files.Write("Users.txt",new[]{"9001|cinema_admin|"+PasswordHelper.Hash("adminPass")+"|admin@cinemago.ge|Admin"});
        if(_files.Read("Movies.txt").Count==0)
            _files.Write("Movies.txt",new[]{"M01|Dune|Sci-Fi|166|12+","M02|The Holdovers|Drama|133|12+"});
        if(_files.Read("Sessions.txt").Count==0)
            _files.Write("Sessions.txt",new[]{"S101|M01|H1|"+DateTime.Now.AddDays(1).Date.AddHours(19).ToString(DateFormat)+"|18.00|30.00|Active",
                                               "S102|M02|H1|"+DateTime.Now.AddDays(2).Date.AddHours(18).ToString(DateFormat)+"|15.00|25.00|Active"});
        foreach(var f in new[]{"Bookings.txt"}) if(_files.Read(f).Count==0)_files.Write(f,Array.Empty<string>());
    }
    private void Load()
    {
        _users.Clear(); _movies.Clear(); _sessions.Clear(); _bookings.Clear();
        foreach(var l in _files.Read("Users.txt")) try { var p=l.Split('|'); User u=p[4].Equals("Admin",StringComparison.OrdinalIgnoreCase)?new Admin():new Customer(); u.Id=int.Parse(p[0]);u.Username=p[1];u.PasswordHash=p[2];u.Email=p[3];u.Role=u is Admin?UserRole.Admin:UserRole.Customer;_users.Add(u);}catch{_log.Log("Bad Users.txt row");}
        foreach(var l in _files.Read("Movies.txt")) try {var p=l.Split('|');_movies.Add(new Movie{Id=p[0],Title=p[1],Genre=p[2],DurationMinutes=int.Parse(p[3]),AgeRating=p[4]});}catch{_log.Log("Bad Movies.txt row");}
        foreach(var l in _files.Read("Sessions.txt")) try {var p=l.Split('|');_sessions.Add(new Session{Id=p[0],MovieId=p[1],HallId=p[2],StartDateTime=DateTime.ParseExact(p[3],DateFormat,CultureInfo.InvariantCulture),StandardPrice=decimal.Parse(p[4],CultureInfo.InvariantCulture),VipPrice=decimal.Parse(p[5],CultureInfo.InvariantCulture),Status=Enum.Parse<SessionStatus>(p[6],true)});}catch{_log.Log("Bad Sessions.txt row");}
        foreach(var l in _files.Read("Bookings.txt")) try {var p=l.Split('|');_bookings.Add(new Booking{Id=p[0],UserId=int.Parse(p[1]),SessionId=p[2],Row=int.Parse(p[3]),Seat=int.Parse(p[4]),TicketType=Enum.Parse<TicketType>(p[5],true),Price=decimal.Parse(p[6],CultureInfo.InvariantCulture),TicketCode=p[7],Status=Enum.Parse<BookingStatus>(p[8],true)});}catch{_log.Log("Bad Bookings.txt row");}
    }
    private void SaveUsers()=>_files.Write("Users.txt",_users.Select(u=>$"{u.Id}|{u.Username}|{u.PasswordHash}|{u.Email}|{u.Role}"));
    private void SaveMovies()=>_files.Write("Movies.txt",_movies.Select(m=>$"{m.Id}|{m.Title}|{m.Genre}|{m.DurationMinutes}|{m.AgeRating}"));
    private void SaveSessions()=>_files.Write("Sessions.txt",_sessions.Select(s=>$"{s.Id}|{s.MovieId}|{s.HallId}|{s.StartDateTime.ToString(DateFormat)}|{s.StandardPrice.ToString("0.00",CultureInfo.InvariantCulture)}|{s.VipPrice.ToString("0.00",CultureInfo.InvariantCulture)}|{s.Status}"));
    private void SaveBookings()=>_files.Write("Bookings.txt",_bookings.Select(b=>$"{b.Id}|{b.UserId}|{b.SessionId}|{b.Row}|{b.Seat}|{b.TicketType}|{b.Price.ToString("0.00",CultureInfo.InvariantCulture)}|{b.TicketCode}|{b.Status}"));
    private void Register()
    {
        Console.Clear(); string name=ReadText("Username: "),email=ReadText("Email: "),pass=ReadText("Password: ");
        if(name.Length<3||pass.Length==0||!email.Contains('@')||_users.Any(u=>u.Username.Equals(name,StringComparison.OrdinalIgnoreCase)||u.Email.Equals(email,StringComparison.OrdinalIgnoreCase)))
        {Console.WriteLine("Invalid or duplicate username/email.");Pause();return;}
        int id=_users.Count==0?1001:_users.Max(u=>u.Id)+1;
        _users.Add(new Customer{Id=id,Username=name,Email=email,PasswordHash=PasswordHelper.Hash(pass),Role=UserRole.Customer});SaveUsers();_log.Log($"Registration: {name}");Console.WriteLine("Registered successfully.");Pause();
    }
    private void Login()
    {
        string name=ReadText("Username: "),pass=ReadText("Password: ");
        _current=_users.FirstOrDefault(u=>u.Username.Equals(name,StringComparison.OrdinalIgnoreCase)&&PasswordHelper.Verify(pass,u.PasswordHash));
        _log.Log(_current==null?$"Failed login: {name}":"Login: "+name);
        if(_current==null){Console.WriteLine("Incorrect login.");Pause();return;}
        _current.DisplayMenu(this);_current=null;
    }
    public void CustomerMenu(Customer c)
    {
        while(true){Console.Clear();Console.WriteLine($"Welcome {c.Username}\n1. Sessions\n2. Show hall and book seat\n3. My bookings / cancel\n4. Search by genre\n5. Popular movie\n0. Logout");
            int x=ReadInt("Choose: ",0,5);if(x==0)return;
            if(x==1){BrowseSessions();Pause();}
            if(x==2)Book(c);
            if(x==3)MyBookings(c);
            if(x==4){string g=ReadText("Genre: ");foreach(var s in _sessions.Where(s=>s.Status==SessionStatus.Active&&_movies.Any(m=>m.Id==s.MovieId&&m.Genre.Contains(g,StringComparison.OrdinalIgnoreCase))))ShowSession(s);Pause();}
            if(x==5){var popular=_bookings.Where(b=>b.Status==BookingStatus.Confirmed).Join(_sessions,b=>b.SessionId,s=>s.Id,(b,s)=>s.MovieId).GroupBy(id=>id).OrderByDescending(g=>g.Count()).FirstOrDefault();Console.WriteLine(popular==null?"No sales yet.":_movies.First(m=>m.Id==popular.Key).Title+" ("+popular.Count()+" tickets)");Pause();}
        }
    }
    public void AdminMenu(Admin a)
    {
        while(true){Console.Clear();Console.WriteLine("=== ADMIN ===\n1. Add movie\n2. Create session\n3. Cancel session\n4. View statistics\n5. View sessions\n6. Edit movie\n7. Delete movie\n8. Edit session\n0. Logout");
            int x=ReadInt("Choose: ",0,8);if(x==0)return;
            if(x==1)AddMovie();if(x==2)AddSession();if(x==3)CancelSession();if(x==4)Statistics();if(x==5){BrowseSessions();Pause();}
            if(x==6)EditMovie();if(x==7)DeleteMovie();if(x==8)EditSession();
        }
    }
    private void BrowseSessions()
    {
        Console.Clear();var list=_sessions.Where(s=>s.Status==SessionStatus.Active).OrderBy(s=>s.StartDateTime).ToList();
        if(list.Count==0){Console.WriteLine("No active sessions.");return;}
        foreach(var s in list)ShowSession(s);
    }
    private void ShowSession(Session s){var m=_movies.FirstOrDefault(m=>m.Id==s.MovieId);if(m!=null)Console.WriteLine($"{s.Id} | {m.Title} | {m.Genre} | {m.DurationMinutes} min | {m.AgeRating} | {s.StartDateTime:yyyy-MM-dd HH:mm} | Hall {s.HallId} | Standard {s.StandardPrice:0.00}₾ / VIP {s.VipPrice:0.00}₾");}
    private void Book(Customer c)
    {
        BrowseSessions(); string sid=ReadText("\nSession ID: ");
        var s=_sessions.FirstOrDefault(x=>x.Id.Equals(sid,StringComparison.OrdinalIgnoreCase)&&x.Status==SessionStatus.Active&&x.StartDateTime>DateTime.Now);
        if(s==null){Console.WriteLine("Session not found or already started.");Pause();return;}
        ShowHall(s); int row=ReadInt("Row: ",1,_halls[s.HallId].Rows),seat=ReadInt("Seat: ",1,_halls[s.HallId].SeatsPerRow);
        if(ActiveBooking(sid,row,seat)!=null){Console.WriteLine("Seat already booked/reserved.");Pause();return;}
        var hall=_halls[s.HallId];bool vip=hall.VipSeats.Contains($"{row}-{seat}");var type=vip?TicketType.VIP:TicketType.Standard;
        decimal basePrice=vip?s.VipPrice:s.StandardPrice;string promo=ReadText("Promo code (Enter to skip; STUDENT=10%, CINEMA20=20%): ").ToUpperInvariant();
        decimal discount=promo=="STUDENT"?10:promo=="CINEMA20"?20:0;
        decimal price=(vip?(Ticket)new VipTicket("PENDING",0):new StandardTicket("PENDING",0)).CalculatePrice(basePrice,discount);
        var b=new Booking{Id="B"+Guid.NewGuid().ToString("N")[..8].ToUpper(),UserId=c.Id,SessionId=s.Id,Row=row,Seat=seat,TicketType=type,Price=price,TicketCode="",Status=BookingStatus.Reserved};
        _bookings.Add(b);SaveBookings();_log.Log($"Seat reserved {b.Id}, user {c.Id}, seat {row}-{seat}");
        Console.WriteLine($"Reservation created. Total: {price:0.00} ₾");
        if(ReadText("Purchase ticket now? (Y/N): ").Equals("Y",StringComparison.OrdinalIgnoreCase))PurchaseBooking(c,b);
        Pause();
    }
    private void PurchaseBooking(Customer c,Booking b)
    {
        if(b.Status!=BookingStatus.Reserved||b.UserId!=c.Id){Console.WriteLine("This reservation cannot be purchased.");return;}
        b.TicketCode=NewTicketCode();b.Status=BookingStatus.Confirmed;SaveBookings();_log.Log("Ticket purchased: "+b.Id);
        var s=_sessions.First(x=>x.Id==b.SessionId);var m=_movies.First(x=>x.Id==s.MovieId);
        Ticket ticket=b.TicketType==TicketType.VIP?(Ticket)new VipTicket(b.TicketCode,b.Price):new StandardTicket(b.TicketCode,b.Price);
        Console.WriteLine("Ticket purchased successfully.");ticket.PrintTicket(s,m,b.Row,b.Seat);
        _email.SendTicket(c.Email,"CinemaGo ticket",$"Movie: {m.Title}\\nSession: {s.StartDateTime:yyyy-MM-dd HH:mm}\\nHall: {s.HallId}\\nSeat: {b.Row}-{b.Seat}\\nPrice: {b.Price:0.00} GEL\\nTicket code: {b.TicketCode}");
    }
    private Booking? ActiveBooking(string sid,int row,int seat)=>_bookings.FirstOrDefault(b=>b.SessionId==sid&&b.Row==row&&b.Seat==seat&&b.Status!=BookingStatus.Cancelled);
    private void ShowHall(Session s)
    {
        var h=_halls[s.HallId];Console.WriteLine("\n[O] Free [X] Booked [V] VIP free [*] VIP booked");
        for(int r=1;r<=h.Rows;r++){Console.Write($"Row {r,2}: ");for(int n=1;n<=h.SeatsPerRow;n++){bool vip=h.VipSeats.Contains($"{r}-{n}");bool booked=ActiveBooking(s.Id,r,n)!=null;Console.Write(booked?(vip?"[*]":"[X]"):(vip?"[V]":"[O]"));}Console.WriteLine();}
    }
    private void MyBookings(Customer c)
    {
        var list=_bookings.Where(b=>b.UserId==c.Id&&b.Status!=BookingStatus.Cancelled).ToList();
        foreach(var b in list)Console.WriteLine($"{b.Id} | {b.SessionId} | {b.Row}-{b.Seat} | {b.Price:0.00}₾ | {(string.IsNullOrEmpty(b.TicketCode)?"Not purchased":b.TicketCode)} | {b.Status}");
        string id=ReadText("Booking ID to manage (Enter to return): ");if(id=="")return;
        var target=list.FirstOrDefault(b=>b.Id.Equals(id,StringComparison.OrdinalIgnoreCase));
        if(target==null){Console.WriteLine("Booking not found.");Pause();return;}
        if(target.Status==BookingStatus.Reserved)
        {
            string action=ReadText("Type BUY to purchase, CANCEL to cancel, or Enter to return: ").ToUpperInvariant();
            if(action=="BUY")PurchaseBooking(c,target);
            else if(action=="CANCEL"){target.Status=BookingStatus.Cancelled;SaveBookings();_log.Log("Reservation cancelled: "+id);Console.WriteLine("Reservation cancelled; seat released.");}
        }
        else if(target.Status==BookingStatus.Confirmed&&ReadText("Cancel purchased ticket? (Y/N): ").Equals("Y",StringComparison.OrdinalIgnoreCase))
        {target.Status=BookingStatus.Cancelled;SaveBookings();_log.Log("Ticket cancelled: "+id);Console.WriteLine("Ticket cancelled; seat released.");}
        Pause();
    }
    private void AddMovie()
    {
        string title=ReadText("Title: "),genre=ReadText("Genre: "),age=ReadText("Age rating: ");int dur=ReadInt("Duration minutes: ",1,500);
        _movies.Add(new Movie{Id="M"+(_movies.Count+1).ToString("D2"),Title=title,Genre=genre,DurationMinutes=dur,AgeRating=age});SaveMovies();_log.Log("Movie added: "+title);
    }
    private void EditMovie()
    {
        foreach(var m in _movies)Console.WriteLine($"{m.Id} | {m.Title} | {m.Genre} | {m.DurationMinutes} | {m.AgeRating}");
        string id=ReadText("Movie ID to edit: ");var movie=_movies.FirstOrDefault(m=>m.Id.Equals(id,StringComparison.OrdinalIgnoreCase));
        if(movie==null){Console.WriteLine("Movie not found.");Pause();return;}
        string title=ReadText($"Title [{movie.Title}]: ");if(title!="")movie.Title=title;
        string genre=ReadText($"Genre [{movie.Genre}]: ");if(genre!="")movie.Genre=genre;
        string age=ReadText($"Age rating [{movie.AgeRating}]: ");if(age!="")movie.AgeRating=age;
        string duration=ReadText($"Duration [{movie.DurationMinutes}]: ");if(duration!=""&&int.TryParse(duration,out int d)&&d>0)movie.DurationMinutes=d;
        SaveMovies();_log.Log("Movie edited: "+id);Console.WriteLine("Movie updated.");Pause();
    }
    private void DeleteMovie()
    {
        foreach(var m in _movies)Console.WriteLine($"{m.Id} | {m.Title}");
        string id=ReadText("Movie ID to delete: ");var movie=_movies.FirstOrDefault(m=>m.Id.Equals(id,StringComparison.OrdinalIgnoreCase));
        if(movie==null){Console.WriteLine("Movie not found.");Pause();return;}
        if(_sessions.Any(s=>s.MovieId==id&&s.Status==SessionStatus.Active)){Console.WriteLine("Cannot delete: active sessions exist. Cancel those sessions first.");Pause();return;}
        _movies.Remove(movie);SaveMovies();_log.Log("Movie deleted: "+id);Console.WriteLine("Movie deleted.");Pause();
    }
    private void EditSession()
    {
        BrowseSessions();string id=ReadText("Session ID to edit: ");var session=_sessions.FirstOrDefault(s=>s.Id.Equals(id,StringComparison.OrdinalIgnoreCase)&&s.Status==SessionStatus.Active);
        if(session==null){Console.WriteLine("Active session not found.");Pause();return;}
        string date=ReadText($"Start [{session.StartDateTime:yyyy-MM-dd HH:mm}]: ");
        DateTime newDate=session.StartDateTime;
        if(date!=""&&(!DateTime.TryParseExact(date,DateFormat,CultureInfo.InvariantCulture,System.Globalization.DateTimeStyles.None,out newDate)||newDate<=DateTime.Now)){Console.WriteLine("Invalid date/time.");Pause();return;}
        if(_sessions.Any(s=>s.Id!=session.Id&&s.HallId==session.HallId&&s.Status==SessionStatus.Active&&Math.Abs((s.StartDateTime-newDate).TotalMinutes)<Math.Max(180,_movies.First(m=>m.Id==s.MovieId).DurationMinutes+20))){Console.WriteLine("Time conflicts with another session in this hall.");Pause();return;}
        string standard=ReadText($"Standard price [{session.StandardPrice}]: ");string vip=ReadText($"VIP price [{session.VipPrice}]: ");
        if(standard!=""&&decimal.TryParse(standard,CultureInfo.InvariantCulture,out var sp)&&sp>0)session.StandardPrice=sp;
        if(vip!=""&&decimal.TryParse(vip,CultureInfo.InvariantCulture,out var vp)&&vp>0)session.VipPrice=vp;
        session.StartDateTime=newDate;SaveSessions();_log.Log("Session edited: "+id);Console.WriteLine("Session updated.");Pause();
    }
    private void AddSession()
    {
        BrowseSessions();string movie=ReadText("Movie ID: ");if(!_movies.Any(m=>m.Id==movie)){Console.WriteLine("Movie missing.");Pause();return;}
        string hall=ReadText("Hall ID (H1): ");if(!_halls.ContainsKey(hall)){Console.WriteLine("Hall missing.");Pause();return;}
        if(!DateTime.TryParseExact(ReadText("Start (yyyy-MM-dd HH:mm): "),DateFormat,null,System.Globalization.DateTimeStyles.None,out var dt)||dt<=DateTime.Now){Console.WriteLine("Invalid/past date.");Pause();return;}
        if(_sessions.Any(s=>s.HallId==hall&&s.Status==SessionStatus.Active&&Math.Abs((s.StartDateTime-dt).TotalMinutes)<Math.Max(180,_movies.First(m=>m.Id==s.MovieId).DurationMinutes+20))){Console.WriteLine("Hall session overlaps (allow 3 hours between sessions).");Pause();return;}
        decimal standard=ReadDecimal("Standard price: ",0.01m,10000),vip=ReadDecimal("VIP price: ",0.01m,10000);
        _sessions.Add(new Session{Id="S"+(_sessions.Count+101),MovieId=movie,HallId=hall,StartDateTime=dt,StandardPrice=standard,VipPrice=vip});SaveSessions();_log.Log("Session created");
    }
    private void CancelSession()
    {
        string id=ReadText("Session ID: ");var s=_sessions.FirstOrDefault(x=>x.Id==id&&x.Status==SessionStatus.Active);if(s==null){Console.WriteLine("Not found.");Pause();return;}
        s.Status=SessionStatus.Cancelled;foreach(var b in _bookings.Where(b=>b.SessionId==id&&b.Status==BookingStatus.Confirmed)){b.Status=BookingStatus.Cancelled;var u=_users.FirstOrDefault(u=>u.Id==b.UserId);if(u!=null)_email.SendTicket(u.Email,"CinemaGo session cancelled",$"Session {id} was cancelled. Your booking was cancelled.");}
        SaveSessions();SaveBookings();_log.Log("Session cancelled: "+id);Console.WriteLine("Session and active bookings cancelled; seats released.");Pause();
    }
    private void Statistics()
    {
        Console.WriteLine("Available seats per active session:");
        foreach(var s in _sessions.Where(s=>s.Status==SessionStatus.Active)){var h=_halls[s.HallId];int total=h.Rows*h.SeatsPerRow;int booked=_bookings.Count(b=>b.SessionId==s.Id&&b.Status==BookingStatus.Confirmed);Console.WriteLine($"{s.Id}: {total-booked}/{total} free");}
        var pop=_bookings.Where(b=>b.Status==BookingStatus.Confirmed).Join(_sessions,b=>b.SessionId,s=>s.Id,(b,s)=>s.MovieId).GroupBy(x=>x).OrderByDescending(g=>g.Count()).FirstOrDefault();
        Console.WriteLine(pop==null?"Most popular: none":$"Most popular: {_movies.First(m=>m.Id==pop.Key).Title} ({pop.Count()})");Pause();
    }
    private static string NewTicketCode()=> "CG-"+Guid.NewGuid().ToString("N")[..6].ToUpper();
    private static string ReadText(string prompt){Console.Write(prompt);return Console.ReadLine()?.Trim()??"";}
    private static int ReadInt(string prompt,int min,int max){while(true){Console.Write(prompt);if(int.TryParse(Console.ReadLine(),out int n)&&n>=min&&n<=max)return n;Console.WriteLine($"Enter a number from {min} to {max}.");}}
    private static decimal ReadDecimal(string prompt,decimal min,decimal max){while(true){Console.Write(prompt);if(decimal.TryParse(Console.ReadLine(),System.Globalization.NumberStyles.Number,CultureInfo.InvariantCulture,out var n)&&n>=min&&n<=max)return n;Console.WriteLine("Enter a valid positive price (use dot for decimals).");}}
    private static void Pause(){Console.WriteLine("\nPress Enter...");Console.ReadLine();}
}
