namespace InkassoAkten.Domain.Entities;

public enum SchuldnerTyp
{
    VerbraucherB2C = 1,
    UnternehmerB2B = 2
}

public enum AkteStatus
{
    Aktenanlage = 1,
    VorgerichtlicheMahnung = 2,
    RatenplanAktiv = 3,
    MahnbescheidBeantragt = 4,
    TituliertUeberwachung = 5,
    Abgeschlossen = 6
}

public class Mandant
{
    public int MandantId { get; set; }
    public string Firmenname { get; set; } = string.Empty;
    public bool IstAktiv { get; set; } = true;
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}

public class Schuldner
{
    public int SchuldnerId { get; set; }
    public SchuldnerTyp SchuldnerTyp { get; set; }
    public string NachnameOderFirma { get; set; } = string.Empty;
    public string? Vorname { get; set; }
    public string Strasse { get; set; } = string.Empty;
    public string PLZ { get; set; } = string.Empty;
    public string Ort { get; set; } = string.Empty;
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}

public class Forderung
{
    public int ForderungId { get; set; }
    public int MandantId { get; set; }
    public Mandant? Mandant { get; set; }
    public int SchuldnerId { get; set; }
    public Schuldner? Schuldner { get; set; }
    public string Rechnungsnummer { get; set; } = string.Empty;
    public decimal Hauptforderung { get; set; }
    public DateOnly Faelligkeitsdatum { get; set; }
    public string Waehrung { get; set; } = "EUR";
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public void Validate()
    {
        if (Hauptforderung <= 0m)
            throw new InvalidOperationException("Hauptforderung must be greater than zero.");
        if (string.IsNullOrWhiteSpace(Waehrung))
            throw new InvalidOperationException("Waehrung must not be empty.");
    }
}

public class InkassoAkte
{
    public int AkteId { get; set; }
    public string Aktenzeichen { get; set; } = string.Empty;
    public int MandantId { get; set; }
    public Mandant? Mandant { get; set; }
    public int SchuldnerId { get; set; }
    public Schuldner? Schuldner { get; set; }
    public int ForderungId { get; set; }
    public Forderung? Forderung { get; set; }
    public AkteStatus AkteStatus { get; set; } = AkteStatus.Aktenanlage;
    public DateOnly VerzugsbeginnDatum { get; set; }
    public DateOnly ZinsenBerechnetBis { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Aktenzeichen))
            throw new InvalidOperationException("Aktenzeichen must not be empty.");
    }
}

public class AuditEintrag
{
    public long AuditId { get; set; }
    public string EntitaetName { get; set; } = string.Empty;
    public int EntitaetId { get; set; }
    public string Aktion { get; set; } = string.Empty;
    public string Akteur { get; set; } = string.Empty;
    public DateTime ZeitstempelUtc { get; set; } = DateTime.UtcNow;
    public string? AltWerteJson { get; set; }
    public string? NeuWerteJson { get; set; }
}
