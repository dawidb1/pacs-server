using System;
using System.Drawing;

namespace pacs_server.Logic
{
    public class BitmapCoder
    {
        public static Bitmap[] gdcmBitmap2Bitmap(gdcm.Bitmap bmjpeg2000)
        {
            // przekonwertuj teraz na bitmapę C#
            uint cols = bmjpeg2000.GetDimension(0);
            uint rows = bmjpeg2000.GetDimension(1);
            uint layers = bmjpeg2000.GetDimension(2);

            // wartość zwracana - tyle obrazków, ile warstw
            Bitmap[] ret = new Bitmap[layers];


            // bufor
            byte[] bufor = new byte[bmjpeg2000.GetBufferLength()];
            if (!bmjpeg2000.GetBuffer(bufor))
                throw new Exception("błąd pobrania bufora");

            // w strumieniu na każdy piksel 2 bajty; tutaj LittleEndian (mnie znaczący bajt wcześniej)
            for (uint l = 0; l < layers; l++)
            {
                Bitmap X = new Bitmap((int)cols, (int)rows);
                double[,] Y = new double[cols, rows];
                double m = 0;

                for (int r = 0; r < rows; r++)
                    for (int c = 0; c < cols; c++)
                    {
                        // współrzędne w strumieniu
                        int j = ((int)(l * rows * cols) + (int)(r * cols) + (int)c) * 2;
                        Y[r, c] = (double)bufor[j + 1] * 256 + (double)bufor[j];
                        // przeskalujemy potem do wartości max.
                        if (Y[r, c] > m)
                            m = Y[r, c];
                    }

                // wolniejsza metoda tworzenia bitmapy
                for (int r = 0; r < rows; r++)
                    for (int c = 0; c < cols; c++)
                    {
                        int f = (int)(255 * (Y[r, c] / m));
                        X.SetPixel(c, r, Color.FromArgb(f, f, f));
                    }
                // kolejna bitmapa
                ret[l] = X;
            }
            return ret;
        }


        // przekonwertuj do formatu bezstratnego JPEG2000
        // na podstawie: http://gdcm.sourceforge.net/html/StandardizeFiles_8cs-example.html
        public static gdcm.Bitmap pxmap2jpeg2000(gdcm.Pixmap px)
        {
            gdcm.ImageChangeTransferSyntax change = new gdcm.ImageChangeTransferSyntax();
            change.SetForce(false);
            change.SetCompressIconImage(false);
            change.SetTransferSyntax(new gdcm.TransferSyntax(gdcm.TransferSyntax.TSType.JPEG2000Lossless));

            change.SetInput(px);
            if (!change.Change())
                throw new Exception("Nie przekonwertowano typu bitmapy na jpeg2000");

            gdcm.Bitmap outimg = change.GetOutputAsBitmap(); // dla GDCM.3.0.4

            return outimg; //change.GetOutput(); // tak było w starszych wersjach
        }

    }
}
