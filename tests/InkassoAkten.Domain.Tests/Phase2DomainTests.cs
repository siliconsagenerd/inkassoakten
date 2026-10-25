using System;
using FluentAssertions;
using InkassoAkten.Domain.Entities;
using InkassoAkten.Domain.Exceptions;
using InkassoAkten.Domain.Strategies;
using InkassoAkten.Domain.ValueObjects;
using Xunit;

namespace InkassoAkten.Domain.Tests;

public class TilgungTests
{
    private readonly StandardTilgungsStrategie _s = new();

    [Fact]
    public void Payment_Smaller_Than_Costs()
    {
        var a = _s.Verrechne(10m, 50m, 20m, 100m);
        a.GetilgteKosten.Should().Be(10m);
        a.GetilgteZinsen.Should().Be(0m);
        a.GetilgteHauptforderung.Should().Be(0m);
        a.NeuerSaldo.Should().Be(160m);
    }

    [Fact]
    public void Payment_Covers_Costs_Plus_Part_Interest()
    {
        var a = _s.Verrechne(60m, 50m, 20m, 100m);
        a.GetilgteKosten.Should().Be(50m);
        a.GetilgteZinsen.Should().Be(10m);
        a.GetilgteHauptforderung.Should().Be(0m);
    }

    [Fact]
    public void Payment_Covers_Costs_Interest_Part_Principal()
    {
        var a = _s.Verrechne(80m, 50m, 20m, 100m);
        a.GetilgteKosten.Should().Be(50m);
        a.GetilgteZinsen.Should().Be(20m);
        a.GetilgteHauptforderung.Should().Be(10m);
    }

    [Fact]
    public void Payment_Exactly_Clears_Balance()
    {
        var a = _s.Verrechne(170m, 50m, 20m, 100m);
        a.GetilgteKosten.Should().Be(50m);
        a.GetilgteZinsen.Should().Be(20m);
        a.GetilgteHauptforderung.Should().Be(100m);
        a.NeuerSaldo.Should().Be(0m);
        (a.GetilgteKosten + a.GetilgteZinsen + a.GetilgteHauptforderung).Should().Be(170m);
    }

    [Fact]
    public void Overpayment_Rejected()
    {
        Action act = () => _s.Verrechne(200m, 50m, 20m, 100m);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Zero_Payment_Rejected()
    {
        Action act = () => _s.Verrechne(0m, 50m, 20m, 100m);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Negative_Payment_Rejected()
    {
        Action act = () => _s.Verrechne(-5m, 50m, 20m, 100m);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Rounding_AwayFromZero()
    {
        var v = decimal.Round(2.345m, 2, MidpointRounding.AwayFromZero);
        v.Should().Be(2.35m);
    }

    [Fact]
    public void Vergleich_Strategy_Prioritizes_Principal()
    {
        var v = new VergleichHauptforderungZuerstStrategie();
        var a = v.Verrechne(80m, 50m, 20m, 100m);
        a.GetilgteHauptforderung.Should().Be(80m);
        a.GetilgteZinsen.Should().Be(0m);
        a.GetilgteKosten.Should().Be(0m);
    }
}

public class ZinsTests
{
    private readonly StandardZinsBerechnungsStrategie _s = new();

    private static readonly BasiszinsPeriode[] Hist = new[]
    {
        new BasiszinsPeriode { GueltigAb = new DateOnly(2025, 1, 1), Basiszinssatz = 2.12m }
    };

    [Fact]
    public void Simple_One_Period()
    {
        var r = _s.Berechne(1000m, new DateTime(2025, 1, 1), new DateTime(2025, 1, 31), SchuldnerTyp.VerbraucherB2C, Hist);
        r.Perioden.Should().HaveCount(1);
        r.Perioden[0].Tage.Should().Be(30);
        r.Perioden[0].AngewandterZinssatz.Should().Be(7.12m);
        r.Gesamtzinsen.Should().Be(5.85m);
    }

    [Fact]
    public void Leap_Year_Day_Count()
    {
        var hist = new[] { new BasiszinsPeriode { GueltigAb = new DateOnly(2024, 1, 1), Basiszinssatz = 2.12m } };
        var r = _s.Berechne(1000m, new DateTime(2024, 2, 1), new DateTime(2024, 3, 1), SchuldnerTyp.VerbraucherB2C, hist);
        r.Perioden[0].Tage.Should().Be(29);
    }

    [Fact]
    public void Base_Rate_Change_Mid_Period_Splits()
    {
        var hist = new[]
        {
            new BasiszinsPeriode { GueltigAb = new DateOnly(2025, 1, 1), Basiszinssatz = 2.12m },
            new BasiszinsPeriode { GueltigAb = new DateOnly(2025, 1, 16), Basiszinssatz = 3.00m }
        };
        var r = _s.Berechne(1000m, new DateTime(2025, 1, 1), new DateTime(2025, 1, 31), SchuldnerTyp.VerbraucherB2C, hist);
        r.Perioden.Should().HaveCount(2);
        r.Perioden[0].Tage.Should().Be(15);
        r.Perioden[1].Tage.Should().Be(15);
    }

    [Fact]
    public void B2C_vs_B2B()
    {
        var b2c = _s.Berechne(1000m, new DateTime(2025, 1, 1), new DateTime(2025, 1, 31), SchuldnerTyp.VerbraucherB2C, Hist);
        var b2b = _s.Berechne(1000m, new DateTime(2025, 1, 1), new DateTime(2025, 1, 31), SchuldnerTyp.UnternehmerB2B, Hist);
        b2b.Gesamtzinsen.Should().BeGreaterThan(b2c.Gesamtzinsen);
        b2c.Perioden[0].AufschlagProzentpunkte.Should().Be(5m);
        b2b.Perioden[0].AufschlagProzentpunkte.Should().Be(9m);
    }

    [Fact]
    public void No_Interest_Before_Verzugsbeginn()
    {
        var r = _s.Berechne(1000m, new DateTime(2025, 2, 1), new DateTime(2025, 1, 31), SchuldnerTyp.VerbraucherB2C, Hist);
        r.Perioden.Should().BeEmpty();
        r.Gesamtzinsen.Should().Be(0m);
    }

    [Fact]
    public void Exact_Day_Count_Boundary()
    {
        var r = _s.Berechne(1000m, new DateTime(2025, 1, 1), new DateTime(2025, 1, 2), SchuldnerTyp.VerbraucherB2C, Hist);
        r.Perioden[0].Tage.Should().Be(1);
    }
}

public class StateMachineTests
{
    [Fact]
    public void Valid_Transition_Aktenanlage_To_VorgerichtlicheMahnung()
    {
        var a = new InkassoAkte { Aktenzeichen = "X" };
        a.StarteVorgerichtlicheMahnung();
        a.AkteStatus.Should().Be(AkteStatus.VorgerichtlicheMahnung);
    }

    [Fact]
    public void Valid_Transition_To_RatenplanAktiv()
    {
        var a = new InkassoAkte { Aktenzeichen = "X" };
        a.StarteVorgerichtlicheMahnung();
        a.AktiviereRatenplan();
        a.AkteStatus.Should().Be(AkteStatus.RatenplanAktiv);
    }

    [Fact]
    public void Invalid_Transition_Throws()
    {
        var a = new InkassoAkte { Aktenzeichen = "X" };
        Action act = () => a.AktiviereRatenplan();
        act.Should().Throw<InvalidTransitionException>();
    }

    [Fact]
    public void Mahnstufe_Cannot_Skip_Levels()
    {
        var a = new InkassoAkte { Aktenzeichen = "X" };
        a.StarteVorgerichtlicheMahnung();
        a.Mahnstufe.Should().Be(1);
        a.ErhoeheMahnstufe();
        a.Mahnstufe.Should().Be(2);
        a.ErhoeheMahnstufe();
        a.Mahnstufe.Should().Be(3);
        Action act = () => a.ErhoeheMahnstufe();
        act.Should().Throw<InvalidTransitionException>();
    }

    [Fact]
    public void Mahnstufe_Raise_Outside_Dunning_Throws()
    {
        var a = new InkassoAkte { Aktenzeichen = "X" };
        Action act = () => a.ErhoeheMahnstufe();
        act.Should().Throw<InvalidTransitionException>();
    }

    [Fact]
    public void Judicial_Escalation_Blocked_By_Active_Plan()
    {
        var a = new InkassoAkte { Aktenzeichen = "X" };
        a.StarteVorgerichtlicheMahnung();
        a.AktiviereRatenplan();
        Action act = () => a.EskaliereZumGerichtlichenMahnverfahren(true);
        act.Should().Throw<InvalidTransitionException>();
    }

    [Fact]
    public void Judicial_Escalation_Allowed_When_No_Active_Plan()
    {
        var a = new InkassoAkte { Aktenzeichen = "X" };
        a.StarteVorgerichtlicheMahnung();
        a.EskaliereZumGerichtlichenMahnverfahren(false);
        a.AkteStatus.Should().Be(AkteStatus.GerichtlichesMahnverfahren);
    }

    [Fact]
    public void Closure_Blocked_With_Outstanding_Balance()
    {
        var a = new InkassoAkte { Aktenzeichen = "X" };
        a.StarteVorgerichtlicheMahnung();
        a.EskaliereZumGerichtlichenMahnverfahren(false);
        a.MarkiereErledigt();
        a.SetzeOffeneBetrage(0m, 0m, 100m);
        Action act = () => a.Schliesse();
        act.Should().Throw<InvalidTransitionException>();
    }

    [Fact]
    public void Closure_Allowed_When_Balance_Zero()
    {
        var a = new InkassoAkte { Aktenzeichen = "X" };
        a.StarteVorgerichtlicheMahnung();
        a.EskaliereZumGerichtlichenMahnverfahren(false);
        a.MarkiereErledigt();
        a.SetzeOffeneBetrage(0m, 0m, 0m);
        a.Schliesse();
        a.AkteStatus.Should().Be(AkteStatus.Geschlossen);
    }

    [Fact]
    public void Close_From_Wrong_State_Throws()
    {
        var a = new InkassoAkte { Aktenzeichen = "X" };
        Action act = () => a.Schliesse();
        act.Should().Throw<InvalidTransitionException>();
    }
}

public class RatenzahlungsplanTests
{
    [Fact]
    public void Valid_Schedule()
    {
        var p = new Ratenzahlungsplan { AkteId = 1, Gesamtbetrag = 100m };
        p.Raten.Add(new Rate { Faelligkeitsdatum = new DateOnly(2025, 2, 1), Betrag = 50m });
        p.Raten.Add(new Rate { Faelligkeitsdatum = new DateOnly(2025, 3, 1), Betrag = 50m });
        Action act = () => p.ValidierePlan();
        act.Should().NotThrow();
    }

    [Fact]
    public void Invalid_Installment_Amount()
    {
        var p = new Ratenzahlungsplan { AkteId = 1, Gesamtbetrag = 100m };
        p.Raten.Add(new Rate { Faelligkeitsdatum = new DateOnly(2025, 2, 1), Betrag = 0m });
        Action act = () => p.ValidierePlan();
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Overlapping_Installments()
    {
        var p = new Ratenzahlungsplan { AkteId = 1, Gesamtbetrag = 100m };
        p.Raten.Add(new Rate { Faelligkeitsdatum = new DateOnly(2025, 2, 1), Betrag = 50m });
        p.Raten.Add(new Rate { Faelligkeitsdatum = new DateOnly(2025, 2, 1), Betrag = 50m });
        Action act = () => p.ValidierePlan();
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Plan_Not_Covering_Balance()
    {
        var p = new Ratenzahlungsplan { AkteId = 1, Gesamtbetrag = 100m };
        p.Raten.Add(new Rate { Faelligkeitsdatum = new DateOnly(2025, 2, 1), Betrag = 30m });
        Action act = () => p.ValidierePlan();
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Default_Detection()
    {
        var p = new Ratenzahlungsplan { AkteId = 1, Gesamtbetrag = 100m };
        p.Raten.Add(new Rate { Faelligkeitsdatum = new DateOnly(2020, 1, 1), Betrag = 100m, IstBezahlt = false });
        p.PruefeDefault(new DateTime(2025, 1, 1));
        p.IstDefaulted.Should().BeTrue();
    }
}
