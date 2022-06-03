using HelperUtilities.Extension;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.Control;

namespace HelperUtilities.Extension
{
    public static class ControlCollectionExtensions
    {
        private static Dictionary<Type, Action<Control>> controldefaults = new Dictionary<Type, Action<Control>>() {
            {typeof(TextBox), c => ((TextBox)c).Clear()},
            {typeof(CheckBox), c => ((CheckBox)c).Checked = false},
            {typeof(ListBox), c => ((ListBox)c).Items.Clear()},
            {typeof(RadioButton), c => ((RadioButton)c).Checked = false},
            {typeof(GroupBox), c => ((GroupBox)c).Controls.ClearControls()},
            {typeof(Panel), c => ((Panel)c).Controls.ClearControls()}
    };
        private static void FindAndInvoke(Type type, Control control, bool enabled = true, bool visible = true)
        {
            if (controldefaults.ContainsKey(type))
            {
                controldefaults[type].Invoke(control);
                control.Enabled = enabled;
                control.Visible = visible;
            }
        }
        public static void ClearControls(this ControlCollection controls, bool enabled = true, bool visible = true)
        {
            foreach (Control control in controls)
            {
                FindAndInvoke(control.GetType(), control, enabled, visible);
            }
        }
        public static void ClearControls<T>(this ControlCollection controls, bool enabled = true, bool visible = true) where T : Control
        {
            if (!controldefaults.ContainsKey(typeof(T))) return;

            foreach (Control control in controls)
            {
                if (control.GetType().Equals(typeof(T)))
                {
                    FindAndInvoke(typeof(T), control, enabled, visible);
                }
            }
        }
        public static List<T> GetChildControls<T>(this ControlCollection controls) where T : Control
        {
            List<T> childControlList = new List<T>();
            foreach (Control control in controls)
                if (control is T)
                    childControlList.Add((T)control);

            return childControlList;
        }
        public static List<T> GetAllChildControls<T>(this ControlCollection controls) where T : Control
        {
            List<T> childControlList = new List<T>();
            foreach (Control control in controls.All().ToList())
                if (control is T)
                    childControlList.Add((T)control);

            return childControlList;
        }
        public static IEnumerable<Control> All(this ControlCollection controls)
        {
            foreach (Control control in controls)
            {
                foreach (Control grandChild in control.Controls.All())
                    yield return grandChild;

                yield return control;
            }
        }

        public static void ForEach(this List<Control> ControlList, Action<Control> Action)
        {
            for (int i = 0; i < ControlList.Count; i++)
            {
                Action.Invoke(ControlList[i]);
            }
        }
        public static void ForEach(this List<Control> ControlList, List<Action<Control>> ActionList)
        {
            for (int i = 0; i < ControlList.Count; i++)
            {
                foreach (Action<Control> action in ActionList)
                {
                    action.Invoke(ControlList[i]);
                }
            }
        }
        public static void ForEach(this ControlCollection ControlCollection, Action<Control> Action)
        {
            for (int i = 0; i < ControlCollection.Count; i++)
            {
                Action.Invoke(ControlCollection[i]);
            }
        }

        public static void ForEach(this ControlCollection ControlCollection, List<Action<Control>> ActionList)
        {
            for (int i = 0; i < ControlCollection.Count; i++)
            {
                foreach (Action<Control> action in ActionList)
                {
                    action.Invoke(ControlCollection[i]);
                }
            }
        }
        public static void For(this ControlCollection ControlCollection, int Start, int End, int Step, Action<Control> Action)
        {
            for (int i = Start; i < End; i++)
            {
                Action.Invoke(ControlCollection[i]);
            }
        }
        public static void For(this ControlCollection ControlCollection, int Start, Func<int, bool> End, int Step, Action<Control> Action)
        {
            for (int i = Start; End.Invoke(i); i++)
            {
                Action.Invoke(ControlCollection[i]);
            }
        }
        public static List<Control> Where(this ControlCollection ControlCollection, Func<Control, bool> Condition, bool SearchAllChildren)
        {
            List<Control> lstControls = new List<Control>();
            ControlCollection.ForEach(ctrl =>
            {
                if (SearchAllChildren)
                {
                    lstControls.AddRange(ctrl.Controls.Where(Condition, true));
                }
                if (Condition.Invoke(ctrl))
                    lstControls.Add(ctrl);
            });
            return lstControls;
        }
        public static List<Control> FindControlsByType(this ControlCollection ControlCollection, Type Type, bool SearchAllChildren)
        {
            List<Control> lstControls = new List<Control>();
            ControlCollection.ForEach(ctrl =>
            {
                if (SearchAllChildren)
                {
                    lstControls.AddRange(ctrl.Controls.FindControlsByType(Type, true));
                }
                if (ctrl.GetType() == Type)
                    lstControls.Add(ctrl);
            });

            return lstControls;
        }
        public static List<Control> ToList(this ControlCollection ControlCollection, bool IncludeChildren)
        {
            List<Control> lstControls = new List<Control>();
            ControlCollection.ForEach(ctrl =>
            {
                if (IncludeChildren)
                    lstControls.AddRange(ctrl.Controls.ToList(true));
                lstControls.Add(ctrl);
            });
            return lstControls;
        }
    }
}
