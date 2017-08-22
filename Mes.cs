using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace CopyFileRAPI
{
    public static class Mes
    {
        public static void Mess_info(string message, string caption)
        {
            MessageBox.Show(message, caption, MessageBoxButtons.OK, MessageBoxIcon.Asterisk, MessageBoxDefaultButton.Button1);
        }

        public static void Mess_info(string message)
        {
            Mess_info(message, "Информация");
        }

        public static DialogResult Mess_confirm(string message, string caption, MessageBoxButtons buttons)
        {
            try
            {
                return MessageBox.Show(message, caption, buttons, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
            }
            catch (Exception)
            {
                return MessageBox.Show(message, caption, buttons, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);
            };
        }

        public static DialogResult Mess_confirm(string message, MessageBoxButtons buttons)
        {
            return Mess_confirm(message, "Подтверждение", buttons);
        }

        /// <summary>
        /// Показывает диалог для подтверждения с двумя кнопками "OK" и "CANCEL"
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public static DialogResult Mess_confirm(string message)
        {
            return Mess_confirm(message, "Подтверждение", MessageBoxButtons.OKCancel);
        }

        public static void Mess_err(string message, string caption)
        {
            MessageBox.Show(message, caption, MessageBoxButtons.OK, MessageBoxIcon.Hand, MessageBoxDefaultButton.Button1);
        }

        public static void Mess_err(string message)
        {
            Mess_err(message, "Ошибка");
        }

        public static void Mess_warn(string message, string caption)
        {
            MessageBox.Show(message, caption, MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1);
        }

        public static void Mess_warn(string message)
        {
            Mess_warn(message, "Предупреждение");
        }
    }
}
