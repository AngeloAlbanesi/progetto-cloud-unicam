using System;
using System.ServiceModel;
using FunzioniDatiHotell.Modelli;

namespace ProvinciaTrentoHotel.WSSoap;

[ServiceContract]
public interface IServizioHotelProvinciaTrento
{
    [OperationContract]
    public Hotel[] DaiElencoHotel();
}

public class ServizioHotelProvinciaTrento : IServizioHotelProvinciaTrento
{
    public Hotel[] DaiElencoHotel()
    {
        return FunzioniDatiHotell.FunzioneInterrogazioneDati.DaiElencoHotel().Result;
    }
}
