using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System.Windows;

namespace OCR
{
    public class UserSetting
    {
        private object m_Owner;
        private object m_Control;

        public UserSetting()
        {
            Key = "";
            Value = "";
            Owner = "";

            ClearControl();
        }

        /// <summary>
        /// Remove the reference to object in this UserSetting
        /// </summary>
        public void ClearControl()
        {
            m_Control = null;
            m_Owner = null;
        }

        /// <summary>
        /// Set the owner of this UserSetting
        /// </summary>
        /// <param name="myOwner"></param>
        public void SetOwner(object myOwner)
        {
            Window Window;
            
            m_Owner = Owner;

            Window = (Window)myOwner;
            Owner = Window.Name;
        }

        /// <summary>
        /// Check if the object is the owner of this UserSetting
        /// </summary>
        /// <param name="CompareOwner"></param>
        /// <returns></returns>
        public bool IsOwner(object CompareOwner)
        {
            return Owner == ((Window)CompareOwner).Name;

        }

        /// <summary>
        /// Set the references of this UserSetting to the given object and update its value
        /// if it is a known control
        /// </summary>
        /// <param name="myControl"></param>
        public void SetControl(object myControl)
        {
            System.Windows.FrameworkElement Control;
            
            m_Control = myControl;

            Control = (System.Windows.FrameworkElement)myControl;
            Key = Control.Name;

            if (Value != "")
            {
                if (m_Control.GetType().ToString() == "System.Windows.Controls.TextBox")
                {
                    ((System.Windows.Controls.TextBox)m_Control).Text = Value;
                }
                if (m_Control.GetType().ToString() == "System.Windows.Controls.CheckBox")
                {
                    ((System.Windows.Controls.CheckBox)m_Control).IsChecked = (Value == "true");
                }
                if (m_Control.GetType().ToString() == "System.Windows.Controls.TabControl")
                {
                    ((System.Windows.Controls.TabControl)m_Control).SelectedIndex = Convert.ToInt32(Value);
                }
                if (m_Control.GetType().ToString() == "System.Windows.Controls.ComboBox")
                {
                    ((System.Windows.Controls.ComboBox)m_Control).SelectedIndex = Convert.ToInt32(Value);
                }
                if (m_Control.GetType().ToString() == "System.Windows.Controls.ListBox")
                {
                    ((System.Windows.Controls.ListBox)m_Control).SelectedIndex = Convert.ToInt32(Value);
                }
                if (m_Control.GetType().ToString() == "System.Windows.Controls.Slider")
                {
                    ((System.Windows.Controls.Slider)m_Control).Value = Convert.ToInt32(Value);
                }
            }
        }

        /// <summary>
        /// Get and store the value of the control in this UserSetting
        /// </summary>
        public void RetrieveValue()
        {
            if (m_Control == null) return;
            
            if (m_Control.GetType().ToString() == "System.Windows.Controls.TextBox")
            {
                Value = ((System.Windows.Controls.TextBox)m_Control).Text;
            }
            if (m_Control.GetType().ToString() == "System.Windows.Controls.CheckBox")
            {
                Value = ((bool)((System.Windows.Controls.CheckBox)m_Control).IsChecked ? "true" : "false");
            }
            if (m_Control.GetType().ToString() == "System.Windows.Controls.TabControl")
            {
                Value = ((System.Windows.Controls.TabControl)m_Control).SelectedIndex.ToString();
            }
            if (m_Control.GetType().ToString() == "System.Windows.Controls.ComboBox")
            {
                Value = ((System.Windows.Controls.ComboBox)m_Control).SelectedIndex.ToString();
            }
            if (m_Control.GetType().ToString() == "System.Windows.Controls.ListBox")
            {
                Value = ((System.Windows.Controls.ListBox)m_Control).SelectedIndex.ToString();
            }
            if (m_Control.GetType().ToString() == "System.Windows.Controls.Slider")
            {
                Value = ((System.Windows.Controls.Slider)m_Control).Value.ToString();
            }
        }

        [XmlAttribute("Owner")]
        public String Owner { get; set; }

        [XmlAttribute("Key")]
        public String Key { get; set; }

        [XmlAttribute("Value")] 
        public String Value { get; set; }
    }    
    
    public class UserSettings
    {
        public UserSettings()
        {
            Settings = new List<UserSetting>(0);
        }
        
        //Load usersettings from the disc
        public static UserSettings Load(String Filename)
        {
            UserSettings newUserSettings = null;

            try
            {
                XmlSerializer s = new XmlSerializer(typeof(UserSettings));
                TextReader r = new StreamReader(Filename);
                newUserSettings = (UserSettings)s.Deserialize(r);
                r.Close();

            }
            catch 
            {
                newUserSettings = new UserSettings();
            }

            newUserSettings.m_filename = Filename;

            return newUserSettings;
        }

        /// <summary>
        /// Save the usersettings to disc
        /// </summary>
        public void Save()
        {
            foreach (UserSetting Setting in Settings)
            {
                Setting.RetrieveValue();
            }
            
            Save(m_filename);
        }
            
        /// <summary>
        /// Save the usersettings to disc with the given filename
        /// </summary>
        /// <param name="Filename"></param>
        public void Save(String Filename)
        {
            StreamWriter w = new StreamWriter(Filename);
            XmlSerializer s = new XmlSerializer(this.GetType());
            s.Serialize(w, this);
            w.Close();
        }

        /// <summary>
        /// This function adds a control to the usersettings. If the setting already existed
        /// it will be attached to its previous value
        /// </summary>
        /// <param name="Owner"></param>
        /// <param name="Control"></param>
        public void AddControl(Object Owner, Object Control)
        {
            UserSetting newSetting = null;

            foreach (UserSetting Setting in Settings)
            {
                if ((Setting.Key == ((System.Windows.FrameworkElement)Control).Name) &&
                    (Setting.Owner == ((Window)Owner).Name))
                {
                    newSetting = Setting;
                }
            }

            if (newSetting == null)
            {
                newSetting = new UserSetting();
                Settings.Add(newSetting);
            }

            newSetting.SetControl(Control);
            newSetting.SetOwner(Owner);
        }

        /// <summary>
        /// This function removes all references to the controls owned by Owner 
        /// in the usersettings list 
        /// </summary>
        /// <param name="Owner"></param>
        public void RemoveControls(Object Owner)
        {
            foreach (UserSetting Setting in Settings)
            {
                if (Setting.IsOwner(Owner))
                {
                    Setting.ClearControl();
                }
            }
        }

        /// <summary>
        /// This function retrieves all values of the controls owned by Owner
        /// </summary>
        /// <param name="Owner"></param>
        public void SaveControlsValue(Object Owner)
        {
            foreach (UserSetting Setting in Settings)
            {
                if (Setting.IsOwner(Owner))
                {
                    Setting.RetrieveValue();
                }
            }
        }

        public List<UserSetting> Settings;

        private string m_filename { get; set; }
    }
}
