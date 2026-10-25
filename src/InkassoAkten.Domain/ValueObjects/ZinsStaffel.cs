namespace InkassoAkten.Domain.ValueObjects;

public record ZinsPeriode(
    DateTime Von,
    DateTime Bis,
    int Tage,
    decimal Basiszinssatz,
    decimal AufschlagProzentpunkte,
    decimal AngewandterZinssatz,
    decimal Zinsen);

public record ZinsStaffel(
    IReadOnlyList<ZinsPeriode> Perioden,
    decimal Gesamtzinsen);
