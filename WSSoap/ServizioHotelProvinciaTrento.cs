using System;
using System.ServiceModel;
using System.Threading.Tasks; // Aggiunto per Task
using FunzioniDatiHotell; // Aggiunto per FunzioneInterrogazioneDati
using FunzioniDatiHotell.Modelli;

namespace ProvinciaTrentoHotel.WSSoap;

[ServiceContract]
public interface IServizioHotelProvinciaTrento
{
    [OperationContract]
    public Task<Hotel[]> DaiElencoHotel(); // Modificato per async

    [OperationContract]

    public Task<Hotel[]> RicercaHotelPerComune(string comune); // Modificato per async
}

public class ServizioHotelProvinciaTrento : IServizioHotelProvinciaTrento
{
    private readonly FunzioneInterrogazioneDati _funzioniDati;

    // Costruttore per Dependency Injection
    public ServizioHotelProvinciaTrento(FunzioneInterrogazioneDati funzioniDati)
    {
        _funzioniDati = funzioniDati;
    }

    public async Task<Hotel[]> DaiElencoHotel()
    {
        // Usa l'istanza iniettata e await
        return await _funzioniDati.DaiElencoHotel();
    }

    public async Task<Hotel[]> RicercaHotelPerComune(string comune)
    {
        // Usa l'istanza iniettata e await
        return await _funzioniDati.RicercaHotelPerComune(comune);
    }
}
