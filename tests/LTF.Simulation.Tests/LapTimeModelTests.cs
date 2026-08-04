using LTF.Domain.Racing;
using LTF.Simulation;
using LTF.Simulation.Laps;
using Xunit;

namespace LTF.Simulation.Tests;

public class LapTimeModelTests
{
    [Fact]
    public void Same_seed_and_inputs_give_the_same_lap()
    {
        var circuit = SimFixtures.Circuit();
        var car = SimFixtures.Car(80);
        var driver = SimFixtures.Attributes(80);

        var a = LapTimeModel.Simulate(circuit, car, driver, SimFixtures.Balance, new DeterministicRandom(9));
        var b = LapTimeModel.Simulate(circuit, car, driver, SimFixtures.Balance, new DeterministicRandom(9));

        Assert.Equal(a, b);
    }

    [Fact]
    public void Total_is_the_sum_of_the_sectors()
    {
        var lap = LapTimeModel.Simulate(
            SimFixtures.Circuit(), SimFixtures.Car(80), SimFixtures.Attributes(80),
            SimFixtures.Balance, new DeterministicRandom(1));

        Assert.Equal(lap.Sector1 + lap.Sector2 + lap.Sector3, lap.Total, 9);
    }

    [Fact]
    public void A_better_car_is_faster_on_average()
    {
        Assert.True(AverageLapForCar(SimFixtures.Car(90)) < AverageLapForCar(SimFixtures.Car(60)));
    }

    [Fact]
    public void A_better_driver_is_faster_on_average()
    {
        Assert.True(AverageLapForDriver(SimFixtures.Attributes(90)) < AverageLapForDriver(SimFixtures.Attributes(60)));
    }

    [Fact]
    public void A_slower_circuit_gives_longer_laps()
    {
        Assert.True(AverageLapForBase(95.0) > AverageLapForBase(70.0));
    }

    private static double AverageLapForCar(Car car)
    {
        var circuit = SimFixtures.Circuit();
        var driver = SimFixtures.Attributes(75);
        var rng = new DeterministicRandom(5);
        return Average(() => LapTimeModel.Simulate(circuit, car, driver, SimFixtures.Balance, rng).Total);
    }

    private static double AverageLapForDriver(DriverAttributes driver)
    {
        var circuit = SimFixtures.Circuit();
        var car = SimFixtures.Car(75);
        var rng = new DeterministicRandom(5);
        return Average(() => LapTimeModel.Simulate(circuit, car, driver, SimFixtures.Balance, rng).Total);
    }

    private static double AverageLapForBase(double baseLap)
    {
        var circuit = SimFixtures.Circuit(baseLap);
        var car = SimFixtures.Car(75);
        var driver = SimFixtures.Attributes(75);
        var rng = new DeterministicRandom(3);
        return Average(() => LapTimeModel.Simulate(circuit, car, driver, SimFixtures.Balance, rng).Total);
    }

    private static double Average(Func<double> lap)
    {
        const int n = 3000;
        double sum = 0;
        for (var i = 0; i < n; i++)
        {
            sum += lap();
        }

        return sum / n;
    }
}
