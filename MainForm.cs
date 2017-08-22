using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using OpenNETCF.Desktop.Communication;
using System.IO;
using System.Reflection;

namespace CopyFileRAPI
{
    public partial class MainForm : Form
    {
        private static readonly string START_TEXT = "Подключите устройство к компьютеру. Для этого установите его на подставку (кредл)";

        private RAPI m_RAPI;
        private ActiveSync m_ActiveSync;

        private string mRemotePath;
        private string mLocalPath;
        //Файлы для инвентаризации
        private string mInputFileInvent;
        private string mInputFileDocsList;

        private string mOutputFileInvent;

        //Файлы для приёмки
        private string mInputFileGoodsList;
        private string mOutputFileMaskAcceptance;

        public static string Version
        {
            get { return Assembly.GetExecutingAssembly().FullName.Split(',')[1].Split('=')[1].ToString(); }
        }

        public MainForm()
        {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            InitializeRAPI();

            this.Text = Version;

            LoadSettings();
        }

        public void InitializeRAPI()
        {
            m_RAPI = new RAPI();

            m_RAPI.RAPIConnected += new RAPIConnectedHandler(RAPI_Connected);
            m_RAPI.RAPIDisconnected += new RAPIConnectedHandler(RAPI_Disconnected);

            //Init rapi if the device is present
            if (m_RAPI.DevicePresent)
                m_RAPI.Connect();
            else
                RAPI_Disconnected();

            m_ActiveSync = m_RAPI.ActiveSync;
            m_ActiveSync.Disconnect += new DisconnectHandler(ActiveSync_Disconnect);
            m_ActiveSync.Active += new ActiveHandler(ActiveSync_Active);
        }

        private void RAPI_Connected()
        {
            this.Invoke(new MethodInvoker(delegate()
            {
                lbStatus.Text = "Устройство подключено. Версия ОС " + DeviceInfo();
                EnableButtons(true);
            }));
        }

        private void EnableButtons(bool value)
        {
            btFromDevice.Enabled = value;
            btOnDevice.Enabled = value;
            tsBtSettings.Enabled = value;
        }

        private void RAPI_Disconnected()
        {
            this.Invoke(new MethodInvoker(delegate()
            {
                lbStatus.Text = START_TEXT;
                EnableButtons(false);
            }));
        }

        private void ActiveSync_Disconnect()
        {
            m_RAPI.Disconnect();
        }

        private void ActiveSync_Active()
        {
            m_RAPI.Connect();
        }

        /// <summary>
        /// Копирует указанный файл с устройства на компьютер
        /// </summary>
        /// <param name="localPath">Путь на компьютере</param>
        /// <param name="remoteFileName">Путь на устройстве</param>
        /// <param name="overwrite">Признак необходимости перезаписи</param>
        /// <returns>TRUE - копирование без ошибок, FALSE - если возникла хоть одна ошибка</returns>
        public bool CopyFromDevice(String localPath, String remoteFileName, bool overwrite)
        {
            bool res = true;
            try
            {
                m_RAPI.CopyFileFromDevice(localPath, remoteFileName, overwrite);
            }
            catch (Exception ex)
            {
                lbStatus.Text = ex.Message;
                res = false;
            }

            return res;
        }

        /// <summary>
        /// Ищем файлы по маске и копируем на локальный компьютер
        /// </summary>
        /// <param name="localPath">Полный путь к папке на локальном компьютере</param>
        /// <param name="remoteFileName">Путь на устройстве с маской поиска</param>
        /// <returns>TRUE - копирование без ошибок, FALSE - если возникла хоть одна ошибка</returns>
        public bool CopyFromDeviceList(String localPath, String remoteFileName)
        {
            string errors = string.Empty;
            bool res = true;
            try
            {
                FileList files = m_RAPI.EnumFiles(remoteFileName);
                foreach (FileInformation item in files)
                {
                    try
                    {
                        m_RAPI.CopyFileFromDevice(mLocalPath + item.FileName, mRemotePath + item.FileName, true);
                        m_RAPI.DeleteDeviceFile(mRemotePath + item.FileName);
                    }
                    catch (Exception exc)
                    {
                        errors = String.Format("{0}\r\n{1}", errors, exc.Message);
                        res = false;
                    }
                }
                if (!res) lbStatus.Text = errors;
            }
            catch (Exception ex)
            {
                lbStatus.Text = ex.Message;
                res = false;
            }
            
            return res;
        }

        public bool CopyToDevice(String localFileName, String remoteFileName)
        {
            bool res = true;
            try
            {
                m_RAPI.CopyFileToDevice(localFileName, remoteFileName, true);
            }
            catch (FileNotFoundException)
            {
                lbStatus.Text = "файл не найден: " + localFileName;
                res = false;
            }
            catch (Exception ex)
            {
                lbStatus.Text = ex.Message;
                res = false;
            }
            return res;
        }

        public bool Connected
        {
            get { return m_RAPI == null ? false : m_RAPI.Connected; }
        }

        public double DeviceInfo()
        {
            if (Connected)
            {
                OSVERSIONINFO os = new OSVERSIONINFO();
                m_RAPI.GetDeviceVersion(out os);
                return os.dwMajorVersion + (os.dwMinorVersion * 0.1);
            }

            return 0f;
        }

        private void btFromDevice_Click(object sender, EventArgs e)
        {
            EnableButtons(false);
            this.Cursor = Cursors.WaitCursor;

            lbStatus.Text = String.Format("Копируем {0}{1} с устройства в {2}{1}", mRemotePath, mOutputFileInvent, mLocalPath);
            string msg;//PRICE_LIST.TXT
            bool res = CopyFromDevice(mLocalPath + mOutputFileInvent, mRemotePath + mOutputFileInvent, true);
            msg = string.Format("Инвентаризация: {0}", res ? " Успех" : "Ошибка, " + mRemotePath + mOutputFileInvent);

            res = CopyFromDevice(mLocalPath + Properties.Settings.Default.OutputFilePriceList, mRemotePath + Properties.Settings.Default.OutputFilePriceList, true);
            msg = string.Format("{0}\r\nЦенники: {1}", msg, res ? " Успех" : "Ошибка, " + mRemotePath + Properties.Settings.Default.OutputFilePriceList);

            res = CopyFromDeviceList(mLocalPath, mRemotePath + mOutputFileMaskAcceptance);
            msg = string.Format("{0}\r\nПриёмка: {1}", msg, res ? "Успех ": "Ошибка при копировании одного или нескольких файлов");
            
            lbStatus.Text = msg;

            this.Cursor = Cursors.Default;
            EnableButtons(true);
        }

        private void btOnDevice_Click(object sender, EventArgs e)
        {
            EnableButtons(false);
            this.Cursor = Cursors.WaitCursor;

            lbStatus.Text = String.Format("Копируем файл {0}{1} и {2} на устройство в {3}",
                mLocalPath, mInputFileInvent, mInputFileGoodsList, mRemotePath);
            string msg;
            try
            {
                bool res = CopyToDevice(mLocalPath + mInputFileInvent, mRemotePath + mInputFileInvent);
                msg = string.Format("Инвентаризация: {0}", res ? "успешно " : "ошибка " + mLocalPath + mInputFileInvent);

                res = CopyToDevice(mLocalPath + mInputFileGoodsList, mRemotePath + mInputFileGoodsList);
                msg = string.Format("{0}\r\nПриёмка: {1}", msg, res ? "успешно" : "ошибка " + mLocalPath + mInputFileInvent);
                
                res = CopyToDevice(mLocalPath + mInputFileDocsList, mRemotePath + mInputFileDocsList);
                msg = string.Format("{0}\r\nДокументы: {1}", msg, res ? "успешно" : "ошибка " + mLocalPath + mInputFileDocsList);
            }
            catch (Exception exc)
            {
                msg = exc.ToString();
            }

            lbStatus.Text = msg;
            
            this.Cursor = Cursors.Default;
            EnableButtons(true);
        }

        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                this.Close();
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (Mes.Mess_confirm("Закрыть программу?") == DialogResult.Cancel){
                e.Cancel = true;
            }else
	        {
                 SaveSettings();
	        }
        }

        private void SaveSettings()
        {
            ReloadSettings(true);
            
            Properties.Settings.Default.Save();
        }

        private void LoadSettings()
        {
            Properties.Settings.Default.Reload();

            ReloadSettings(false);
        }

        private void ReloadSettings(bool direction)
        {
            if (direction)
            {
                Properties.Settings.Default.LocalPath = mLocalPath;
                Properties.Settings.Default.RemotePath = mRemotePath;
                
                Properties.Settings.Default.InputFileInvent = mInputFileInvent;
                Properties.Settings.Default.InputFileDocsList = mInputFileDocsList;
                Properties.Settings.Default.InputFileGoodsList = mInputFileGoodsList;
                
                Properties.Settings.Default.OutputFileInvent = mOutputFileInvent;
                Properties.Settings.Default.OutputFileMaskAcceptance = mOutputFileMaskAcceptance;
            }
            else
            {
                mLocalPath = Properties.Settings.Default.LocalPath;
                mRemotePath = Properties.Settings.Default.RemotePath;

                mInputFileInvent = Properties.Settings.Default.InputFileInvent;
                mInputFileDocsList = Properties.Settings.Default.InputFileDocsList;
                mInputFileGoodsList = Properties.Settings.Default.InputFileGoodsList;

                mOutputFileInvent = Properties.Settings.Default.OutputFileInvent;
                mOutputFileMaskAcceptance = Properties.Settings.Default.OutputFileMaskAcceptance;
            }
        }

        private void tsBtSettings_Click(object sender, EventArgs e)
        {
            SettingsForm sf = new SettingsForm();
            
            ReloadSettings(sf.ShowDialog() != DialogResult.OK);
        }
    }
}
