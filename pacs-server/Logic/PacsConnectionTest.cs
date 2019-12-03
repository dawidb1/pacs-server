using System;
using System.Collections.Generic;
using System.Drawing;

namespace pacs_server.Logic
{
    public class PacsConnectionTest
    {
        static string myAET = "KLIENTL";       // moj AET - ustaw zgodnie z konfiguracją serwera PACS
        static string callAET = "ARCHIWUM";    // AET serwera - j.w.
        static string ipPACS = "127.0.0.1";    // IP serwera - j.w.
        static ushort portPACS = 10100;        // port serwera - j.w.
        static ushort portMove = 10104;        // port zwrotny dla MOVE - j.w.

        public static string Echo()
        {
            //ECHO
            bool stan = gdcm.CompositeNetworkFunctions.CEcho(ipPACS, portPACS, myAET, callAET);
            if (stan)
                return "ECHO działa.";
            else
                return "ECHO nie działa!";
        }

        public static string Store(string fileName)
        {
            // STORE
            // dodaj listę plików
            gdcm.FilenamesType pliki = new gdcm.FilenamesType();
            pliki.Add(fileName);

            // wyślij
            bool stan = gdcm.CompositeNetworkFunctions.CStore(ipPACS, portPACS, pliki, myAET, callAET);

            if (stan)
                return "STORE działa.";
            else
                return "STORE nie działa!";
        }

        public static string Find()
        {
            string resultString = "";
            //FIND

            // typ wyszukiwania (rozpoczynamy od pacjenta)
            gdcm.ERootType typ = gdcm.ERootType.ePatientRootType;

            // do jakiego poziomu wyszukujemy 
            gdcm.EQueryLevel poziom = gdcm.EQueryLevel.ePatient; // zobacz tez inne 

            // klucze (filtrowanie lub określenie, które dane są potrzebne)
            gdcm.KeyValuePairArrayType klucze = new gdcm.KeyValuePairArrayType();

            gdcm.Tag tag = new gdcm.Tag(0x0010, 0x0010); // 10,10 == PATIENT_NAME
            gdcm.KeyValuePairType klucz1 = new gdcm.KeyValuePairType(tag, "*"); // * == dowolne imię
            klucze.Add(klucz1);
            klucze.Add(new gdcm.KeyValuePairType(new gdcm.Tag(0x0010, 0x0020), ""));
            // zwrotnie oczekujemy wypełnionego 10,20 czyli PATIENT_ID

            // skonstruuj zapytanie
            gdcm.BaseRootQuery zapytanie = gdcm.CompositeNetworkFunctions.ConstructQuery(typ, poziom, klucze);

            // sprawdź, czy zapytanie spełnia kryteria
            if (!zapytanie.ValidateQuery())
            {
                return "FIND błędne zapytanie!";
            }

            // kontener na wyniki
            gdcm.DataSetArrayType wynik = new gdcm.DataSetArrayType();

            // wykonaj zapytanie
            bool stan = gdcm.CompositeNetworkFunctions.CFind(ipPACS, portPACS, zapytanie, wynik, myAET, callAET);

            // sprawdź stan
            if (!stan)
            {
                return "FIND nie działa!";
            }

            resultString += "FIND działa.";

            // pokaż wyniki
            foreach (gdcm.DataSet x in wynik)
            {
                resultString += x.toString(); // cała odpowiedź jako wielolinijkowy napis
                // UWAGA: toString() vs ToString() !!!

                // + DOSTEP DO METADANYCH
                //for (var iter = x.Begin(); iter != x.End(); ++iter) { } // brak wrapowania iteratorów...

                // jeden element pary klucz-wartość
                gdcm.DataElement de = x.GetDataElement(new gdcm.Tag(0x0010, 0x0020)); // konkretnie 10,20 = PATIENT_ID

                // dostęp jako string
                gdcm.Value val = de.GetValue(); // pobierz wartość dla wskazanego klucza...
                string str = val.toString();    // ...jako napis
                resultString += "ID Pacjenta: " + str;

                // dostęp jako tablica bajtów
                gdcm.ByteValue bval = de.GetByteValue(); // pobierz jako daną binarną
                byte[] buff = new byte[bval.GetLength().GetValueLength()]; // przygotuj tablicę bajtów
                bval.GetBuffer(buff, (uint)buff.Length); // skopiuj zawartość
                // a co z tym dalej zrobić to już inna kwestia...

                //Console.WriteLine();
            }
            return resultString;
        }

        public static string Move()
        {
            string resultString = string.Empty;
            // typ wyszukiwania (rozpoczynamy od pacjenta)
            gdcm.ERootType typ = gdcm.ERootType.ePatientRootType;

            // do jakiego poziomu wyszukujemy 
            gdcm.EQueryLevel poziom = gdcm.EQueryLevel.ePatient; // zobacz inne 

            // klucze (filtrowanie lub określenie, które dane są potrzebne)
            gdcm.KeyValuePairArrayType klucze = new gdcm.KeyValuePairArrayType();
            gdcm.KeyValuePairType klucz1 = new gdcm.KeyValuePairType(new gdcm.Tag(0x0010, 0x0020), "01"); // NIE WOLNO TU STOSOWAC *; tutaj PatientID="01"
            klucze.Add(klucz1);

            // skonstruuj zapytanie
            gdcm.BaseRootQuery zapytanie = gdcm.CompositeNetworkFunctions.ConstructQuery(typ, poziom, klucze, gdcm.EQueryType.eMove);

            // sprawdź, czy zapytanie spełnia kryteria
            if (!zapytanie.ValidateQuery())
            {
                return "MOVE błędne zapytanie!";
            }

            // przygotuj katalog na wyniki
            string odebrane = System.IO.Path.Combine(".", "odebrane"); // podkatalog odebrane w bieżącym katalogu
            if (!System.IO.Directory.Exists(odebrane)) // jeśli nie istnieje
                System.IO.Directory.CreateDirectory(odebrane); // utwórz go
            string dane = System.IO.Path.Combine(odebrane, System.IO.Path.GetRandomFileName()); // wygeneruj losową nazwę podkatalogu
            System.IO.Directory.CreateDirectory(dane); // i go utwórz

            // wykonaj zapytanie - pobierz do katalogu jak w zmiennej 'dane'
            bool stan = gdcm.CompositeNetworkFunctions.CMove(ipPACS, portPACS, zapytanie, portMove, myAET, callAET, dane);

            // sprawdź stan
            if (!stan)
            {
                return "MOVE nie działa!";
            }

            resultString += "MOVE działa.";

            List<string> pliki = new List<string>(System.IO.Directory.EnumerateFiles(dane));
            foreach (string plik in pliki)
            {
                resultString += "pobrano: {0}" + plik;

                // MOVE + konwersja
                // przeczytaj pixele
                gdcm.PixmapReader reader = new gdcm.PixmapReader();
                reader.SetFileName(plik);
                if (!reader.Read())
                {
                    // najpewniej nie jest to obraz
                    resultString += "pomijam: {0}" + plik;
                    continue;
                }

                // przekonwertuj na "znany format"
                gdcm.Bitmap bmjpeg2000 = BitmapCoder.pxmap2jpeg2000(reader.GetPixmap());
                // przekonwertuj na .NET bitmapę
                Bitmap[] X = BitmapCoder.gdcmBitmap2Bitmap(bmjpeg2000);
                // zapisz
                for (int i = 0; i < X.Length; i++)
                {
                    string name = String.Format("{0}_warstwa{1}.jpg", plik, i);
                    X[i].Save(name);
                    resultString += "konwersja do: {0}" + name;
                }
            }
            return resultString;
        }
    }
}
