using System;
using System.ServiceModel;
using FunzioniDatiHotell.Modelli;

namespace ProvinciaTrentoHotel.WSSoap;

[ServiceContract]
public interface IServizioHotelProvinciaTrento
{
    [OperationContract]
    public Hotel[] DaiElencoHotel();

    [OperationContract]

    public Hotel[] RicercaHotelPerComune(string comune);
}

public class ServizioHotelProvinciaTrento : IServizioHotelProvinciaTrento
{
    public Hotel[] DaiElencoHotel()
    {
        return FunzioniDatiHotell.FunzioneInterrogazioneDati.DaiElencoHotel().Result;
    }

    public Hotel[] RicercaHotelPerComune(string comune)
    {
        return FunzioniDatiHotell.FunzioneInterrogazioneDati.RicercaHotelPerComune(comune).Result;
    }
}
