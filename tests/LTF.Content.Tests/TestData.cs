namespace LTF.Content.Tests;

internal static class TestData
{
    /// <summary>
    /// A minimal but fully valid carset.json: one team, two seated drivers, one circuit,
    /// one calendar round. The exact field text is relied on by the loader tests, which
    /// tweak single tokens to produce invalid variants.
    /// </summary>
    public const string MinimalValid =
        """
        {
          "id": "t",
          "name": "T",
          "rules": { "seriesName": "S", "points": { "racePoints": [25, 18, 15] } },
          "tyres": [ { "compound": "Soft", "code": "C4", "grip": 90, "durability": 40 } ],
          "circuits": [
            { "id": "c1", "name": "C1", "laps": 50, "lapDistanceKm": 5.0, "baseLapTimeSeconds": 80.0 }
          ],
          "calendar": [ { "round": 1, "circuitId": "c1", "date": "2025-03-16" } ],
          "teams": [
            {
              "id": "tm", "name": "Team",
              "car": { "aerodynamics": 80, "chassis": 80, "powerUnit": 80, "tyreGentleness": 80, "reliability": 80 },
              "driverIds": [ "d1", "d2" ]
            }
          ],
          "drivers": [
            { "id": "d1", "firstName": "A", "lastName": "B", "age": 25,
              "attributes": { "pace": 80, "racecraft": 81, "consistency": 82, "tyreManagement": 83, "wetWeather": 84, "feedback": 85 } },
            { "id": "d2", "firstName": "C", "lastName": "D", "age": 26,
              "attributes": { "pace": 70, "racecraft": 71, "consistency": 72, "tyreManagement": 73, "wetWeather": 74, "feedback": 75 } }
          ]
        }
        """;
}
