using Estud.Back.Domain.Campi;

namespace Estud.Back.Features.Campi.GetCampi;

public static class GetCampiMapper
{
    extension(Campus campus)
    {
        public GetCampiItemOut ToGetCampiItemOut(decimal usedMinutesRate, decimal usedCapacityRate)
        {
            return new()
            {
                Id = campus.Id,
                Name = campus.Name,
                City = campus.City,
                State = campus.State,
                UsedMinutesRate = usedMinutesRate,
                UsedCapacityRate = usedCapacityRate,
            };
        }
    }
}
