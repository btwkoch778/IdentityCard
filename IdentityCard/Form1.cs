using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml.Linq;

namespace identitycardaze
{
    public partial class Form1 : Form
    {
        private Image photo;

        public Form1()
        {
            InitializeComponent();

            txtAd.MaxLength = txtSoyad.MaxLength = txtAtaAdi.MaxLength = 80;
            txtFin.MaxLength = 7;
            txtSeriya.MaxLength = 11;
            txtDogumTarixi.MaxLength = txtVerilmeTarixi.MaxLength = 10;
            txtEtibarlidir.MaxLength = 10;
            txtDogumYeri.MaxLength = 150;
            txtVerenOrqan.MaxLength = 150;
            txtFin.CharacterCasing = CharacterCasing.Upper;
            txtSeriya.CharacterCasing = CharacterCasing.Upper;

            cmbCins.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbCins.Items.Clear();
            cmbCins.Items.AddRange(new object[] { "Kişi", "Qadın" });
            cmbCins.SelectedIndex = -1;

            cmbAileVeziyyeti.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbAileVeziyyeti.Items.Clear();
            cmbAileVeziyyeti.Items.AddRange(
                new object[] { "Subay", "Evli", "Boşanmış", "Dul" });
            cmbAileVeziyyeti.SelectedIndex = -1;

            epXeta.ContainerControl = this;
            epXeta.BlinkStyle = ErrorBlinkStyle.NeverBlink;
            picVesiqe.SizeMode = PictureBoxSizeMode.Zoom;
            picVesiqe.Paint += Preview_Paint;
            btnSekilYukle.Click += btnSekilYukle_Click;
            btnTemizle.Click += btnTemizle_Click;
            btnYaddaSaxla.Click += btnYaddaSaxla_Click;

            foreach (TextBox field in TextFields())
                field.TextChanged += Input_Changed;

            txtAd.KeyPress += Name_KeyPress;
            txtSoyad.KeyPress += Name_KeyPress;
            txtAtaAdi.KeyPress += Name_KeyPress;
            txtFin.KeyPress += Fin_KeyPress;
            txtSeriya.KeyPress += Serial_KeyPress;
            cmbCins.SelectedIndexChanged += Input_Changed;
            cmbAileVeziyyeti.SelectedIndexChanged += Input_Changed;
            Disposed += Form1_Disposed;

            tipKomek.SetToolTip(txtSeriya, "AA1234567 və ya AZE12345678; boşluqsuz.");
            tipKomek.SetToolTip(txtFin, "7 simvol: 0–9 və ingilis hərfləri; I və O olmaz.");
            tipKomek.SetToolTip(txtDogumTarixi, "Gün.ay.il — məsələn, 17.01.2007.");
            tipKomek.SetToolTip(txtVerilmeTarixi, "Gün.ay.il — məsələn, 01.06.2025.");
            tipKomek.SetToolTip(txtEtibarlidir, "Gün.ay.il və ya Müddətsiz.");
            AcceptButton = btnYaddaSaxla;
        }

        private TextBox[] TextFields()
        {
            return new[] { txtSeriya, txtFin, txtSoyad, txtAd, txtAtaAdi,
                txtDogumTarixi, txtDogumYeri, txtVerilmeTarixi,
                txtEtibarlidir, txtVerenOrqan };
        }

        private void Input_Changed(object sender, EventArgs e)
        {
            Control field = sender as Control;
            if (field != null)
                epXeta.SetError(field, string.Empty);
            picVesiqe.Invalidate();
        }

        private void Name_KeyPress(object sender, KeyPressEventArgs e)
        {
            // Ctrl+V daxil olmaqla idarəetmə düymələrinə icazə verilir.
            // Yapışdırılmış mətn yadda saxlama zamanı ayrıca yoxlanılır.
            if (!char.IsControl(e.KeyChar) && !char.IsLetter(e.KeyChar)
                && e.KeyChar != ' ' && e.KeyChar != '-'
                && e.KeyChar != '\'' && e.KeyChar != '\u2019')
                e.Handled = true;
        }

        private void Fin_KeyPress(object sender, KeyPressEventArgs e)
        {
            char c = char.ToUpperInvariant(e.KeyChar);
            if (!char.IsControl(e.KeyChar)
                && !((c >= '0' && c <= '9')
                     || (c >= 'A' && c <= 'Z' && c != 'I' && c != 'O')))
                e.Handled = true;
        }

        private void Serial_KeyPress(object sender, KeyPressEventArgs e)
        {
            char c = char.ToUpperInvariant(e.KeyChar);
            if (!char.IsControl(e.KeyChar)
                && !((c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z')))
                e.Handled = true;
        }

        private void NormalizeInputs()
        {
            foreach (TextBox field in TextFields())
                field.Text = ValidationRules.Clean(field.Text);
            txtFin.Text = txtFin.Text.ToUpperInvariant();
            txtSeriya.Text = txtSeriya.Text.ToUpperInvariant();
        }

        private void Check(Control field, bool valid, string message,
                           ref Control firstError, StringBuilder messages)
        {
            if (valid)
                return;
            epXeta.SetError(field, message);
            if (firstError == null)
                firstError = field;
            messages.AppendLine("• " + message);
        }

        private bool ValidateForm(bool showMessage)
        {
            NormalizeInputs();
            epXeta.Clear();
            Control firstError = null;
            StringBuilder messages = new StringBuilder();

            Check(txtSeriya, ValidationRules.IsSerial(txtSeriya.Text),
                "Seriya və nömrə: AA1234567 və ya AZE12345678 formatını yazın.",
                ref firstError, messages);
            Check(txtFin, ValidationRules.IsFin(txtFin.Text),
                "FİN 7 simvol olmalıdır: 0–9 və ingilis hərfləri; I və O olmaz.",
                ref firstError, messages);
            Check(txtSoyad, ValidationRules.IsName(txtSoyad.Text),
                "Soyadı hərflərlə yazın; rəqəm olmaz. Defis və apostrof mümkündür.",
                ref firstError, messages);
            Check(txtAd, ValidationRules.IsName(txtAd.Text),
                "Adı hərflərlə yazın; rəqəm olmaz. Defis və apostrof mümkündür.",
                ref firstError, messages);
            Check(txtAtaAdi, ValidationRules.IsName(txtAtaAdi.Text),
                "Ata adını hərflərlə yazın; rəqəm olmaz.", ref firstError, messages);
            Check(cmbCins, cmbCins.SelectedIndex >= 0,
                "Cinsi siyahıdan seçin.", ref firstError, messages);

            DateTime birth, issued, expires;
            bool birthOk = ValidationRules.TryDate(txtDogumTarixi.Text, out birth);
            bool issuedOk = ValidationRules.TryDate(txtVerilmeTarixi.Text, out issued);
            bool unlimited = ValidationRules.IsUnlimited(txtEtibarlidir.Text);
            bool expiresOk = ValidationRules.TryDate(txtEtibarlidir.Text, out expires);

            Check(txtDogumTarixi, birthOk && birth <= DateTime.Today,
                "Doğum tarixi düzgün gün.ay.il formatında olmalı və gələcəkdə olmamalıdır.",
                ref firstError, messages);
            Check(txtDogumYeri, ValidationRules.IsDescription(txtDogumYeri.Text),
                "Doğum yerini yazın; sahə boş və ya yalnız rəqəmlərdən ibarət ola bilməz.",
                ref firstError, messages);
            Check(cmbAileVeziyyeti, cmbAileVeziyyeti.SelectedIndex >= 0,
                "Ailə vəziyyətini siyahıdan seçin.", ref firstError, messages);
            Check(txtVerilmeTarixi,
                issuedOk && issued <= DateTime.Today && (!birthOk || issued >= birth),
                "Verilmə tarixi düzgün olmalı, doğumdan əvvəl və gələcəkdə olmamalıdır.",
                ref firstError, messages);
            Check(txtEtibarlidir,
                unlimited || (expiresOk && expires >= DateTime.Today
                              && (!issuedOk || expires > issued)),
                "Etibarlılıq: gün.ay.il və ya Müddətsiz yazın. Müddət bitməməli, tarix verilmədən sonra olmalıdır.",
                ref firstError, messages);
            Check(txtVerenOrqan, ValidationRules.IsDescription(txtVerenOrqan.Text),
                "Vəsiqəni verən orqanı yazın; sahə boş və ya yalnız rəqəmlər ola bilməz.",
                ref firstError, messages);
            Check(btnSekilYukle, photo != null,
                "Şəkil yüklə düyməsi ilə foto seçin.", ref firstError, messages);

            if (firstError == null)
                return true;

            if (showMessage)
                MessageBox.Show(this, messages.ToString(), "Məlumatları yoxlayın",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            firstError.Focus();
            return false;
        }

        private void btnSekilYukle_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Title = "Foto seçin";
                dialog.Filter = "Şəkillər (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp";
                dialog.CheckFileExists = true;
                dialog.Multiselect = false;
                if (dialog.ShowDialog(this) != DialogResult.OK)
                    return;

                try
                {
                    Image loaded = LoadPhoto(dialog.FileName);
                    Image old = photo;
                    photo = loaded;
                    if (old != null)
                        old.Dispose();
                    epXeta.SetError(btnSekilYukle, string.Empty);
                    picVesiqe.Invalidate();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, "Şəkil yüklənmədi.\n" + ex.Message,
                        "Şəkil xətası", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        internal static Image LoadPhoto(string path)
        {
            using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                if (stream.Length == 0 || stream.Length > 20L * 1024 * 1024)
                    throw new InvalidDataException("Şəkil boş olmamalı və 20 MB-dan böyük olmamalıdır.");

                using (Image source = Image.FromStream(stream, true, true))
                {
                    Guid format = source.RawFormat.Guid;
                    if (format != ImageFormat.Jpeg.Guid && format != ImageFormat.Png.Guid
                        && format != ImageFormat.Bmp.Guid)
                        throw new InvalidDataException("Yalnız JPG, PNG və BMP şəkilləri qəbul edilir.");
                    if ((long)source.Width * source.Height > 24000000)
                        throw new InvalidDataException("Şəkil 24 meqapikseldən böyük olmamalıdır.");

                    double scale = Math.Min(1.0, 1000.0 / Math.Max(source.Width, source.Height));
                    Bitmap copy = new Bitmap(Math.Max(1, (int)(source.Width * scale)),
                                             Math.Max(1, (int)(source.Height * scale)));
                    try
                    {
                        using (Graphics graphics = Graphics.FromImage(copy))
                        {
                            graphics.Clear(Color.White);
                            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                            graphics.DrawImage(source, 0, 0, copy.Width, copy.Height);
                        }
                        return copy; // Axın bağlandıqdan sonra da şəkil işləyir.
                    }
                    catch
                    {
                        copy.Dispose();
                        throw;
                    }
                }
            }
        }

        private void btnTemizle_Click(object sender, EventArgs e)
        {
            foreach (TextBox field in TextFields())
                field.Clear();
            cmbCins.SelectedIndex = cmbAileVeziyyeti.SelectedIndex = -1;
            if (photo != null)
            {
                photo.Dispose();
                photo = null;
            }
            epXeta.Clear();
            picVesiqe.Invalidate();
            txtSeriya.Focus();
        }

        private XDocument CreateDocument()
        {
            DateTime birth, issued, expires;
            ValidationRules.TryDate(txtDogumTarixi.Text, out birth);
            ValidationRules.TryDate(txtVerilmeTarixi.Text, out issued);
            ValidationRules.TryDate(txtEtibarlidir.Text, out expires);
            string encodedPhoto;
            using (MemoryStream stream = new MemoryStream())
            {
                photo.Save(stream, ImageFormat.Png);
                encodedPhoto = Convert.ToBase64String(stream.ToArray());
            }

            return new XDocument(new XDeclaration("1.0", "utf-8", null),
                new XElement("SexsiyyetVesiqesi",
                    new XElement("SeriyaVeNomre", txtSeriya.Text),
                    new XElement("FIN", txtFin.Text),
                    new XElement("Soyad", txtSoyad.Text),
                    new XElement("Ad", txtAd.Text),
                    new XElement("AtaAdi", txtAtaAdi.Text),
                    new XElement("Cins", cmbCins.Text),
                    new XElement("DogumTarixi", birth.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
                    new XElement("DogumYeri", txtDogumYeri.Text),
                    new XElement("AileVeziyyeti", cmbAileVeziyyeti.Text),
                    new XElement("VerilmeTarixi", issued.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
                    new XElement("Etibarlidir", ValidationRules.IsUnlimited(txtEtibarlidir.Text)
                        ? "Müddətsiz" : expires.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
                    new XElement("VerenOrqan", txtVerenOrqan.Text),
                    new XElement("Foto", new XAttribute("format", "PNG"), encodedPhoto)));
        }

        private void btnYaddaSaxla_Click(object sender, EventArgs e)
        {
            if (!ValidateForm(true))
                return;

            using (SaveFileDialog dialog = new SaveFileDialog())
            {
                dialog.Title = "Qeydiyyatı yadda saxla";
                dialog.Filter = "XML faylı (*.xml)|*.xml";
                dialog.DefaultExt = "xml";
                dialog.AddExtension = true;
                dialog.OverwritePrompt = true;
                dialog.FileName = "Vesiqe_" + txtFin.Text + ".xml";
                if (dialog.ShowDialog(this) != DialogResult.OK)
                    return;

                try
                {
                    ValidationRules.SaveDocument(CreateDocument(), dialog.FileName);
                    MessageBox.Show(this, "Məlumatlar və foto yadda saxlanıldı.\n" + dialog.FileName,
                        "Hazırdır", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, "Yadda saxlamaq mümkün olmadı.\n" + ex.Message,
                        "Fayl xətası", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void Preview_Paint(object sender, PaintEventArgs e)
        {
            if (picVesiqe.ClientSize.Width <= 20 || picVesiqe.ClientSize.Height <= 20)
                return;
            Graphics graphics = e.Graphics;
            GraphicsState state = graphics.Save();
            try
            {
                float scale = Math.Min((picVesiqe.ClientSize.Width - 20) / 1000f,
                                       (picVesiqe.ClientSize.Height - 20) / 630f);
                graphics.TranslateTransform((picVesiqe.ClientSize.Width - 1000 * scale) / 2,
                                            (picVesiqe.ClientSize.Height - 630 * scale) / 2);
                graphics.ScaleTransform(scale, scale);
                graphics.SmoothingMode = SmoothingMode.AntiAlias;

                using (SolidBrush background = new SolidBrush(Color.FromArgb(223, 235, 247)))
                using (Pen border = new Pen(Color.SteelBlue, 4))
                using (Font title = new Font("Segoe UI", 24, FontStyle.Bold))
                using (Font subtitle = new Font("Segoe UI", 16))
                {
                    graphics.FillRectangle(background, 2, 2, 996, 626);
                    graphics.DrawRectangle(border, 2, 2, 996, 626);
                    graphics.DrawString("AZƏRBAYCAN RESPUBLİKASI", title, Brushes.MidnightBlue, 32, 25);
                    graphics.DrawString("Şəxsiyyət vəsiqəsi", subtitle, Brushes.MidnightBlue, 32, 75);

                    RectangleF box = new RectangleF(32, 135, 195, 250);
                    graphics.FillRectangle(Brushes.LightSteelBlue, box);
                    if (photo == null)
                        graphics.DrawString("FOTO", title, Brushes.SteelBlue, 72, 235);
                    else
                    {
                        float zoom = Math.Min(box.Width / photo.Width, box.Height / photo.Height);
                        float width = photo.Width * zoom, height = photo.Height * zoom;
                        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        graphics.DrawImage(photo, box.X + (box.Width - width) / 2,
                            box.Y + (box.Height - height) / 2, width, height);
                    }
                    graphics.DrawRectangle(Pens.SteelBlue, box.X, box.Y, box.Width, box.Height);
                }

                string[] leftLabels = { "Soyad", "Ad", "Ata adı", "Doğum tarixi", "Doğum yeri", "Cins" };
                string[] leftValues = { txtSoyad.Text, txtAd.Text, txtAtaAdi.Text,
                    txtDogumTarixi.Text, txtDogumYeri.Text, cmbCins.Text };
                string[] rightLabels = { "Seriya və nömrə", "FİN", "Ailə vəziyyəti",
                    "Verilmə tarixi", "Etibarlıdır", "Verən orqan" };
                string[] rightValues = { txtSeriya.Text, txtFin.Text, cmbAileVeziyyeti.Text,
                    txtVerilmeTarixi.Text, txtEtibarlidir.Text, txtVerenOrqan.Text };
                using (Font label = new Font("Segoe UI", 14))
                using (Font value = new Font("Segoe UI", 17, FontStyle.Bold))
                using (StringFormat format = new StringFormat())
                {
                    format.Trimming = StringTrimming.EllipsisCharacter;
                    format.FormatFlags = StringFormatFlags.NoWrap;
                    for (int i = 0; i < 6; i++)
                    {
                        float y = 135 + i * 75;
                        DrawPreviewField(graphics, leftLabels[i], leftValues[i], 260, y, 345, label, value, format);
                        DrawPreviewField(graphics, rightLabels[i], rightValues[i], 635, y, 330, label, value, format);
                    }
                }
            }
            finally
            {
                graphics.Restore(state);
            }
        }

        private static void DrawPreviewField(Graphics graphics, string label, string value,
            float x, float y, float width, Font labelFont, Font valueFont, StringFormat format)
        {
            graphics.DrawString(label + ":", labelFont, Brushes.SteelBlue,
                new RectangleF(x, y, width, 26), format);
            if (!string.IsNullOrWhiteSpace(value))
            {
                graphics.DrawString(value, valueFont, Brushes.MidnightBlue,
                    new RectangleF(x, y + 27, width, 36), format);
            }
            graphics.DrawLine(Pens.SteelBlue, x, y + 64, x + width - 8, y + 64);
        }

        private void Form1_Disposed(object sender, EventArgs e)
        {
            if (photo != null)
            {
                photo.Dispose();
                photo = null;
            }
        }
    }

    // Eyni Form1.cs faylındadır; ayrıca NuGet paketi lazım deyil.
    internal static class ValidationRules
    {
        internal static string Clean(string text)
        {
            return Regex.Replace((text ?? string.Empty).Normalize(NormalizationForm.FormC).Trim(), @"\s+", " ");
        }

        internal static bool IsName(string text)
        {
            return Regex.IsMatch(text ?? string.Empty, @"\A\p{L}+(?:[ '\u2019-]\p{L}+)*\z");
        }

        internal static bool IsFin(string text)
        {
            return Regex.IsMatch(text ?? string.Empty, @"\A[0-9A-HJ-NP-Z]{7}\z");
        }

        internal static bool IsSerial(string text)
        {
            return Regex.IsMatch(text ?? string.Empty, @"\A(?:[A-Z]{2}[0-9]{7}|AZE[0-9]{8})\z");
        }

        internal static bool TryDate(string text, out DateTime date)
        {
            return DateTime.TryParseExact(text, "dd.MM.yyyy", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out date);
        }

        internal static bool IsUnlimited(string text)
        {
            return CultureInfo.GetCultureInfo("az-Latn-AZ").CompareInfo.Compare(
                text, "Müddətsiz", CompareOptions.IgnoreCase) == 0;
        }

        internal static bool IsDescription(string text)
        {
            return !string.IsNullOrWhiteSpace(text) && Regex.IsMatch(text, @"\p{L}")
                && !Regex.IsMatch(text, @"\p{C}");
        }

        internal static void SaveDocument(XDocument document, string path)
        {
            string target = Path.GetFullPath(path);
            string temporary = Path.Combine(Path.GetDirectoryName(target),
                ".vesiqe_" + Guid.NewGuid().ToString("N") + ".tmp");
            try
            {
                document.Save(temporary);
                if (File.Exists(target))
                    File.Replace(temporary, target, null);
                else
                    File.Move(temporary, target);
            }
            finally
            {
                if (File.Exists(temporary))
                {
                    try { File.Delete(temporary); }
                    catch (IOException) { }
                    catch (UnauthorizedAccessException) { }
                }
            }
        }
    }
}
