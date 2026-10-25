using InkassoAkten.Domain.Exceptions;

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
    GerichtlichesMahnverfahren = 4,
    Erledigt = 5,
    Geschlossen = 6
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
    public int Mahnstufe { get; private set; } = 1;
    public DateOnly VerzugsbeginnDatum { get; set; }
    public DateOnly ZinsenBerechnetBis { get; set; }
    public decimal OffeneKosten { get; private set; }
    public decimal OffeneZinsen { get; private set; }
    public decimal OffeneHauptforderung { get; private set; }
    public decimal Gesamtsaldo => OffeneKosten + OffeneZinsen + OffeneHauptforderung;
    public decimal Restforderung => Gesamtsaldo;
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    public List<Ratenzahlungsplan> Ratenzahlungsplaene { get; set; } = new();

    public void SetzeOffeneBetrage(decimal kosten, decimal zinsen, decimal hauptforderung)
    {
        OffeneKosten = kosten;
        OffeneZinsen = zinsen;
        OffeneHauptforderung = hauptforderung;
    }

    public void Validiere() { if (string.IsNullOrWhiteSpace(Aktenzeichen)) throw new InvalidOperationException("Aktenzeichen must not be empty."); }
    public void Validate() => Validiere();

    // state machine
    public void StarteVorgerichtlicheMahnung()
    {
        if (AkteStatus != AkteStatus.Aktenanlage)
            throw new InvalidTransitionException($"Cannot start dunning from {AkteStatus}.");
        AkteStatus = AkteStatus.VorgerichtlicheMahnung;
    }

    public void ErhoeheMahnstufe()
    {
        if (AkteStatus != AkteStatus.VorgerichtlicheMahnung)
            throw new InvalidTransitionException("Mahnstufe can only be raised during dunning.");
        if (Mahnstufe >= 3)
            throw new InvalidTransitionException("Mahnstufe cannot exceed 3.");
        Mahnstufe += 1;
    }

    public void AktiviereRatenplan()
    {
        if (AkteStatus != AkteStatus.VorgerichtlicheMahnung)
            throw new InvalidTransitionException($"Cannot activate installment plan from {AkteStatus}.");
        AkteStatus = AkteStatus.RatenplanAktiv;
    }

    public void EskaliereZumGerichtlichenMahnverfahren(bool hatAktivenNichtDefaultedPlan)
    {
        if (hatAktivenNichtDefaultedPlan)
            throw new InvalidTransitionException("Cannot escalate while an active, non-defaulted payment plan exists.");
        if (AkteStatus != AkteStatus.VorgerichtlicheMahnung && AkteStatus != AkteStatus.RatenplanAktiv)
            throw new InvalidTransitionException($"Cannot escalate from {AkteStatus}.");
        AkteStatus = AkteStatus.GerichtlichesMahnverfahren;
    }

    public void MarkiereErledigt()
    {
        if (AkteStatus != AkteStatus.GerichtlichesMahnverfahren && AkteStatus != AkteStatus.RatenplanAktiv && AkteStatus != AkteStatus.VorgerichtlicheMahnung)
            throw new InvalidTransitionException($"Cannot mark completed from {AkteStatus}.");
        AkteStatus = AkteStatus.Erledigt;
    }

    public void Schliesse()
    {
        if (AkteStatus != AkteStatus.Erledigt)
            throw new InvalidTransitionException($"Cannot close from {AkteStatus}.");
        if (Restforderung > 0m)
            throw new InvalidTransitionException("Cannot close Akte while Restforderung > 0.");
        AkteStatus = AkteStatus.Geschlossen;
    }

    public bool HatAktivenNichtDefaultedRatenplan()
    {
        return Ratenzahlungsplaene.Any(p => !p.IstDefaulted);
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
