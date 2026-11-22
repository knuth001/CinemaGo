# CinemaGo — Complete classroom project
C# / .NET 8 Console Application for Visual Studio 2022.

## Open and run
1. Download and extract `CinemaGo_Project.zip`.
2. Open `CinemaGo/CinemaGo.csproj` in Visual Studio 2022.
3. Restore/build if prompted; press **Ctrl+F5**.
4. The application creates `Data` files on first run.

## Demo administrator
Username: `cinema_admin`
Password: `adminPass`

The demo admin is seeded only if `Users.txt` does not exist or is empty. Change it before real use.

## Included
- Customer registration/login, Admin/Customer role-based menus
- PBKDF2 password hashing
- Movie and session browsing
- Hall layout; free [O], booked [X], free VIP [V], booked VIP [*]
- Seat reservation, ticket purchase, cancellation and unique ticket codes
- Standard/VIP ticket polymorphism and promo codes STUDENT (10%), CINEMA20 (20%)
- Admin add/edit/delete movie, create/edit/cancel session
- Session cancellation automatically cancels active bookings and sends cancellation email when SMTP is configured
- LINQ filtering and popular-film/seat statistics
- Persistent pipe-separated TXT data files and operation log
- Input validation and exception handling for malformed data and SMTP errors

## Data format
Data is created under the program's output directory in `Data/`:
- `Users.txt`
- `Movies.txt`
- `Sessions.txt`
- `Bookings.txt`
- `CinemaGo.log`

User passwords are stored as PBKDF2 hashes instead of plain text. The remaining columns follow the assignment's pipe-separated format. User IDs start at 1001 and the seeded Admin ID is 9001.

## SMTP setup (optional)
Set these environment variables in Windows before launching:
- `CINEMAGO_SMTP_HOST`
- `CINEMAGO_SMTP_PORT` (optional, defaults to 587)
- `CINEMAGO_SMTP_USER`
- `CINEMAGO_SMTP_PASSWORD`

For Gmail, use an app password where applicable. Do not write credentials into the source code. If SMTP is not configured, the program logs that email was skipped and continues.

## Notes
- Hall H1 has 6 rows × 10 seats; first two rows' seats 5 and 6 are sample VIP seats.
- Sample sessions are generated for future dates on first run.
- This is a console application because the assignment explicitly requires console hall visualization.
