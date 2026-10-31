namespace FlomiApp.Services;

/// <summary>Warum eine Anmeldung für einen Bereich nicht möglich ist.</summary>
public enum RegistrationBlock
{
    None,
    AreaNotFound,
    AlreadyRegistered,  // dieselbe Person ist für diesen Bereich schon angemeldet
    FullDayOnly,        // Halbtag gewählt, die Stufe darf hier aber nur ganze Tage (AreaTemplate.MandatoryStufen)
    MinAge,
    Full,
    TimeConflict,       // zur gleichen Zeit schon in einem anderen Bereich eingetragen
    SaleLimit,          // pro Event nur ein Verkaufsbereich
    CakeLimit,          // pro Tag nur ein Kuchen
}

/// <summary>
/// Ergebnis von <see cref="IAppointmentService.CheckRegistrationAsync"/>. Bei <see cref="RegistrationBlock.TimeConflict"/>,
/// <see cref="RegistrationBlock.SaleLimit"/> und <see cref="RegistrationBlock.CakeLimit"/> steht in <c>Other*</c>
/// die bestehende Anmeldung, die im Weg ist. Den Text dazu baut <c>Helpers/RegistrationMessages</c>.
/// </summary>
public record RegistrationCheck(
    RegistrationBlock Block,
    int?      MinAge        = null,
    int?      PersonAge     = null,
    string?   OtherAreaName = null,
    DateTime? OtherDate     = null,
    string?   OtherTimeSlot = null)
{
    public static readonly RegistrationCheck Ok = new(RegistrationBlock.None);

    public bool CanRegister => Block == RegistrationBlock.None;
}
