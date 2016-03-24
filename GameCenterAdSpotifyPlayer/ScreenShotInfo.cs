using System.Windows.Forms;

namespace GameCenterAdNotifier
{
    public class ScreenShotInfo
    {
        public int Width { get; private set; }

        public int Height { get; private set; }

        public int XOffSet { get; private set; }

        public int YOffSet { get; private set; }


        public ScreenShotInfo(Screen screen)
        {
            float screenRes = screen.Bounds.Size.Width / screen.Bounds.Size.Height;

            if (screenRes == Program.StreamRes)
            {
                Width = screen.Bounds.Size.Width;
                Height = screen.Bounds.Size.Height;
                XOffSet = 0;
                YOffSet = 0;
            }
            else if (screenRes > Program.StreamRes)
            {
                //wider than 16:9 (Black bars on sides)
                Height = screen.Bounds.Size.Height;
                float heightRatio = (float) Height / (float)1080;
                YOffSet = 0;

                float width = (float)1920 * (float)heightRatio;

                Width = (int)width;
                XOffSet = (screen.Bounds.Size.Width - Width) / 2;
            }
            else if (screenRes < Program.StreamRes)
            {
                //Taller than 16:9 (Black bars on top and bottom)
                Width = screen.Bounds.Size.Width;
                float widthRatio = (float)Width / (float)1920;
                XOffSet = 0;

                float height = (float)1080 * (float)widthRatio;

                Height = (int)height;
                YOffSet = (screen.Bounds.Size.Height - Height) / 2;
            }
        }

    }

}