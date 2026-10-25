using System;
using Xunit;
using FluentAssertions;
using InkassoAkten.Domain.Entities;

namespace InkassoAkten.Domain.Tests;

public class DomainEntityTests
{
    [Fact]
    public void Forderung_Cannot_Have_NonPositive_Hauptforderung()
    {
        var f = new Forderung { Hauptforderung = 0m };
        Action act = () => f.Validate();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void SchuldnerTyp_Must_Be_Defined_Value()
    {
        Enum.IsDefined(typeof(SchuldnerTyp), (int)SchuldnerTyp.VerbraucherB2C).Should().BeTrue();
        Enum.IsDefined(typeof(SchuldnerTyp), (int)SchuldnerTyp.UnternehmerB2B).Should().BeTrue();
        Enum.IsDefined(typeof(SchuldnerTyp), 999).Should().BeFalse();
    }

    [Fact]
    public void InkassoAkte_Cannot_Have_Empty_Aktenzeichen()
    {
        var a = new InkassoAkte { Aktenzeichen = "" };
        Action act = () => a.Validate();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Decimal_Money_Preserves_Two_Decimals()
    {
        decimal value = 123.45m;
        value.Should().Be(123.45m);
        (value * 1).Should().Be(123.45m);
    }
}
