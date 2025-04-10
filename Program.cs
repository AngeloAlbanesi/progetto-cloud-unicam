/*
 *   Copyright (c) 2025 
 *   All rights reserved.
 */
using FunzioniDatiHotell;
using ProvinciaTrentoHotel.WSSoap;
using SoapCore;

namespace ProvinciaTrentoHotel
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddSoapCore();
            builder.Services.AddScoped<
                IServizioHotelProvinciaTrento,
                ServizioHotelProvinciaTrento
            >();

            // Aggiungi il servizio di memorizzazione nella cache
            builder.Services.AddMemoryCache();
            builder.Services.AddControllersWithViews();
            // Registro FunzioneInterrogazioneDati per la DI
            builder.Services.AddScoped<FunzioneInterrogazioneDati>();

            var app = builder.Build();

            // Rimuovo l'impostazione statica di BaseUrl, ora gestita tramite DI in FunzioneInterrogazioneDati

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}"
            );

            app.UseEndpoints(endpoints =>
            {
                endpoints.UseSoapEndpoint<IServizioHotelProvinciaTrento>(
                    "/ServizioHotelProvinciaTrento.wsdl",
                    new SoapEncoderOptions(),
                    SoapSerializer.XmlSerializer
                );
            });
            app.Run();
        }
    }
}
