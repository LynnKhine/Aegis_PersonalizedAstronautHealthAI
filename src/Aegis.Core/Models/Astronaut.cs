namespace Aegis.Core.Models;

public class Astronaut
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NASAId { get; set; } = string.Empty;
    public DateTime MissionStartDate { get; set; }

    public ICollection<BiometricReading> BiometricReadings { get; set; } = new List<BiometricReading>();
    public ICollection<PersonalBaseline> PersonalBaselines { get; set; } = new List<PersonalBaseline>();
    public ICollection<InterventionPlan> InterventionPlans { get; set; } = new List<InterventionPlan>();
}
