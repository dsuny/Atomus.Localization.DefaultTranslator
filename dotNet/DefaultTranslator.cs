using Atomus.Control.Localization.Controllers;
using Atomus.Diagnostics;
using Atomus.Service;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Windows;

namespace Atomus.Localization
{
    public class DefaultTranslator : ITranslator
    {
        Dictionary<string, DataSet> ITranslator.Dictionary { get; set; }

        private readonly Dictionary<System.Windows.Forms.Control, string> controls;
        private readonly Dictionary<DependencyObject, string> dependencyObjects;
        private ArrayList SkipControlFullNames;

        private List<string> OtherLetterListTotal = new List<string>();
        private List<string> OtherLetterList = new List<string>();
        private int OtherLetterListBufferCount;

        string ITranslator.SourceCultureName { get; set; }
        string ITranslator.TargetCultureName { get; set; }

        System.Globalization.CultureInfo ITranslator.SourceCultureInfo
        {
            set
            {
                ((ITranslator)this).SourceCultureName = value.Name;
            }
        }
        System.Globalization.CultureInfo ITranslator.TargetCultureInfo
        {
            set
            {
                ((ITranslator)this).TargetCultureName = value.Name;
            }
        }

        public DefaultTranslator()
        {
            ((ITranslator)this).Dictionary = new Dictionary<string, DataSet>();
            this.controls = new Dictionary<System.Windows.Forms.Control, string>();
            this.dependencyObjects = new Dictionary<DependencyObject, string>();
            this.SkipControlFullNames = new ArrayList();

            try
            {
                ((ITranslator)this).SourceCultureName = this.GetAttribute("SourceCultureName");
            }
            catch (Exception ex)
            {
                DiagnosticsTool.MyTrace(ex);
            }

            try
            {
                this.SkipControlFullNames.AddRange(this.GetAttribute("SkipControlFullNames").Split(','));
            }
            catch (Exception ex)
            {
                DiagnosticsTool.MyTrace(ex);
            }

            try
            {
                this.OtherLetterListBufferCount = this.GetAttributeInt("OtherLetterListBufferCount");
            }
            catch (Exception ex)
            {
                DiagnosticsTool.MyTrace(ex);
                this.OtherLetterListBufferCount = -1;
            }
        }

        string ITranslator.Translate(string source, params string[] args)
        {
            return ((ITranslator)this).Translate(source, ((ITranslator)this).SourceCultureName, ((ITranslator)this).TargetCultureName, args);
        }
        string ITranslator.Translate(string source, System.Globalization.CultureInfo sourceCultureInfo, System.Globalization.CultureInfo targetCultureInfo, params string[] args)
        {
            return ((ITranslator)this).Translate(source, sourceCultureInfo.Name, targetCultureInfo.Name, args);
        }

        string ITranslator.Translate(string source, string sourceCultureName, string targetCultureName, params string[] args)
        {
            DataSet dataSet;
            DataTable dataTable;

            try
            {
                if (!((ITranslator)this).Dictionary.ContainsKey(sourceCultureName))
                    this.LoadDictionary(sourceCultureName, targetCultureName);

                if (((ITranslator)this).Dictionary.ContainsKey(sourceCultureName))
                {
                    dataSet = ((ITranslator)this).Dictionary[sourceCultureName];

                    if (!dataSet.Tables.Contains(targetCultureName))
                        this.LoadDictionary(sourceCultureName, targetCultureName);

                    dataTable = dataSet.Tables[targetCultureName];

                    if (dataTable == null || dataTable.Rows.Count < 1)
                        this.UnicodeCategoryCheckAndSave(source, sourceCultureName);
                    else
                        foreach (DataRow _DataRow in dataTable.Rows)
                        {
                            if (((string)_DataRow[0]).Equals(source))
                            {
                                if (args == null || args.Length == 0)
                                    return (string)_DataRow[1];
                                else
                                    return string.Format((string)_DataRow[1], ((ITranslator)this).Translate(args));
                            }
                            else
                                this.UnicodeCategoryCheckAndSave(source, sourceCultureName);
                        }
                }
            }
            catch (Exception exception)
            {
                DiagnosticsTool.MyTrace(exception);
            }
            finally
            { }

            if (args == null || args.Length == 0)
                return source;
            else
                return string.Format(source, ((ITranslator)this).Translate(args));
        }

        private void UnicodeCategoryCheckAndSave(string source, string sourceCultureName)
        {
            char[] chars;

            if (this.OtherLetterListBufferCount <= 0)
                return;

            chars = source.ToCharArray();

            foreach (char ch in chars)
                if (Char.GetUnicodeCategory(ch) == System.Globalization.UnicodeCategory.OtherLetter)
                {
                    if (!this.OtherLetterListTotal.Contains(source))
                    {
                        this.OtherLetterListTotal.Add(source);
                        this.OtherLetterList.Add(source);
                    }
                    break;
                }

            if (this.OtherLetterList.Count >= this.OtherLetterListBufferCount)
            {
                this.SaveAsync(this.OtherLetterList.ToArray(), sourceCultureName);
                this.OtherLetterList.Clear();
            }
        }

        string[] ITranslator.Translate(string[] source, params string[][] args)
        {
            return ((ITranslator)this).Translate(source, ((ITranslator)this).SourceCultureName, ((ITranslator)this).TargetCultureName, args);
        }
        string[] ITranslator.Translate(string[] source, System.Globalization.CultureInfo sourceCultureInfo, System.Globalization.CultureInfo targetCultureInfo, params string[][] args)
        {
            return ((ITranslator)this).Translate(source, sourceCultureInfo.Name, targetCultureInfo.Name, args);
        }
        string[] ITranslator.Translate(string[] source, string sourceCultureName, string targetCultureName, params string[][] args)
        {
            if (source == null)
                return source;

            for (int i = 0; i < source.Length; i++)
            {
                source[i] = ((ITranslator)this).Translate(source[i], sourceCultureName, targetCultureName, (args != null && args.Length != 0) ? args[i] : null);
            }

            return source;
        }

        DataTable ITranslator.Translate(DataTable source)
        {
            return ((ITranslator)this).Translate(source, ((ITranslator)this).SourceCultureName, ((ITranslator)this).TargetCultureName);
        }
        DataTable ITranslator.Translate(DataTable source, System.Globalization.CultureInfo sourceCultureInfo, System.Globalization.CultureInfo targetCultureInfo)
        {
            return ((ITranslator)this).Translate(source, sourceCultureInfo.Name, targetCultureInfo.Name);
        }
        DataTable ITranslator.Translate(DataTable source, string sourceCultureName, string targetCultureName)
        {
            foreach (DataColumn dataColumn in source.Columns)
            {
                dataColumn.Caption = ((ITranslator)this).Translate(dataColumn.Caption, sourceCultureName, targetCultureName);
            }

            return source;
        }
        DataSet ITranslator.Translate(DataSet source)
        {
            return ((ITranslator)this).Translate(source, ((ITranslator)this).SourceCultureName, ((ITranslator)this).TargetCultureName);
        }
        DataSet ITranslator.Translate(DataSet source, System.Globalization.CultureInfo sourceCultureInfo, System.Globalization.CultureInfo targetCultureInfo)
        {
            return ((ITranslator)this).Translate(source, sourceCultureInfo.Name, targetCultureInfo.Name);
        }
        DataSet ITranslator.Translate(DataSet source, string sourceCultureName, string targetCultureName)
        {
            foreach (DataTable dataTable in source.Tables)
            {
                ((ITranslator)this).Translate(dataTable, sourceCultureName, targetCultureName);
            }

            return source;
        }

        void ITranslator.Translate(System.Windows.Forms.Control control)
        {
            ((ITranslator)this).Translate(control, ((ITranslator)this).SourceCultureName, ((ITranslator)this).TargetCultureName);
        }
        void ITranslator.Translate(System.Windows.Forms.Control control, System.Globalization.CultureInfo sourceCultureInfo, System.Globalization.CultureInfo targetCultureInfo)
        {
            ((ITranslator)this).Translate(control, sourceCultureInfo.Name, targetCultureInfo.Name);
        }
        void ITranslator.Translate(System.Windows.Forms.Control control, string sourceCultureName, string targetCultureName)
        {
            if (control is System.Windows.Forms.ListControl)
                return;

            if (control is System.Windows.Forms.TextBoxBase)
                return;

            if (control is System.Windows.Forms.WebBrowserBase)
                return;

            if (control is System.Windows.Forms.WebBrowserBase)
                return;

            if (this.SkipControlFullNames.Contains(control.GetType().FullName))
                return;

            if (!this.controls.ContainsKey(control))
                this.controls.Add(control, control.Text);

            if (control.Controls != null && control.Controls.Count > 0)
            {
                ((ITranslator)this).Translate(control.Controls, sourceCultureName, targetCultureName);
            }

            //Text 속석을 사용 할 수 없는 컨트롤도 있음
            try
            {
                control.Text = ((ITranslator)this).Translate(this.controls[control], sourceCultureName, targetCultureName);
            }
            catch (Exception exception)
            {
                DiagnosticsTool.MyTrace(exception);
            }
        }
        void ITranslator.Translate(System.Windows.Forms.Control.ControlCollection controls)
        {
            ((ITranslator)this).Translate(controls, ((ITranslator)this).SourceCultureName, ((ITranslator)this).TargetCultureName);
        }
        void ITranslator.Translate(System.Windows.Forms.Control.ControlCollection controls, System.Globalization.CultureInfo sourceCultureInfo, System.Globalization.CultureInfo targetCultureInfo)
        {
            foreach (System.Windows.Forms.Control control in controls)
            {
                ((ITranslator)this).Translate(control, sourceCultureInfo, targetCultureInfo);
            }
        }
        void ITranslator.Translate(System.Windows.Forms.Control.ControlCollection controls, string sourceCultureName, string targetCultureName)
        {
            foreach (System.Windows.Forms.Control control in controls)
            {
                ((ITranslator)this).Translate(control, sourceCultureName, targetCultureName);
            }
        }
        void ITranslator.Translate(System.Windows.Forms.ContainerControl containerControl)
        {
            ((ITranslator)this).Translate(containerControl, ((ITranslator)this).SourceCultureName, ((ITranslator)this).TargetCultureName);
        }
        void ITranslator.Translate(System.Windows.Forms.ContainerControl containerControl, System.Globalization.CultureInfo sourceCultureInfo, System.Globalization.CultureInfo targetCultureInfo)
        {
            ((ITranslator)this).Translate(containerControl, sourceCultureInfo.Name, targetCultureInfo.Name);
        }
        void ITranslator.Translate(System.Windows.Forms.ContainerControl containerControl, string sourceCultureName, string targetCultureName)
        {
            foreach (System.Windows.Forms.Control control in containerControl.Controls)
            {
                ((ITranslator)this).Translate(control, sourceCultureName, targetCultureName);
            }
        }

        void ITranslator.Restoration(System.Windows.Forms.Control control)
        {
            if (this.controls.ContainsKey(control))
            {
                if (control.Controls != null && control.Controls.Count > 0)
                {
                    ((ITranslator)this).Restoration(control.Controls);
                }

                //Text 속석을 사용 할 수 없는 컨트롤도 있음
                try
                {
                    control.Text = this.controls[control];
                }
                catch (Exception _Exception)
                {
                    DiagnosticsTool.MyTrace(_Exception);
                }

                this.controls.Remove(control);
            }
        }
        void ITranslator.Restoration(System.Windows.Forms.Control.ControlCollection controls)
        {
            foreach (System.Windows.Forms.Control control in controls)
            {
                ((ITranslator)this).Restoration(control);
            }
        }
        void ITranslator.Restoration(System.Windows.Forms.ContainerControl containerControl)
        {
            foreach (System.Windows.Forms.Control control in containerControl.Controls)
            {
                ((ITranslator)this).Restoration(control);
            }
        }

        private void LoadDictionary(string sourceCultureName, string targetCultureName)
        {
            IResponse result;
            DataTable dataTable;
            DataSet dataSet;

            try
            {
                if (sourceCultureName == targetCultureName || sourceCultureName == null || targetCultureName == null)
                    return;

                //if (sourceCultureName == null || targetCultureName == null)
                //    return;

                result = this.Search(new Control.Localization.Models.DefaultTranslatorSearchModel()
                {
                    SOURCE_LANGUAGE_TYPE = sourceCultureName,
                    TARGET_LANGUAGE_TYPE = targetCultureName
                });

                if (result.Status == Status.OK)
                {
                    if (result.DataSet.Tables.Count < 1)
                    {
                        return;
                    }

                    if (!((ITranslator)this).Dictionary.ContainsKey(sourceCultureName))
                    {
                        dataSet = new DataSet();
                        ((ITranslator)this).Dictionary.Add(sourceCultureName, dataSet);
                    }

                    dataSet = ((ITranslator)this).Dictionary[sourceCultureName];

                    if (!dataSet.Tables.Contains(targetCultureName))
                    {
                        dataTable = result.DataSet.Tables[0];
                        result.DataSet.Tables.Remove(dataTable);
                        dataTable.TableName = targetCultureName;

                        dataSet.Tables.Add(dataTable);
                    }
                }
            }
            catch (AtomusException exception)
            {
                DiagnosticsTool.MyTrace(exception);
            }
            catch (Exception exception)
            {
                DiagnosticsTool.MyTrace(exception);
            }
        }



        void ITranslator.Translate(DependencyObject dependencyObject)
        {
            ((ITranslator)this).Translate(dependencyObject, ((ITranslator)this).SourceCultureName, ((ITranslator)this).TargetCultureName);
        }
        void ITranslator.Translate(DependencyObject dependencyObject, CultureInfo sourceCultureInfo, CultureInfo targetCultureInfo)
        {
            ((ITranslator)this).Translate(dependencyObject, sourceCultureInfo.Name, targetCultureInfo.Name);
        }
        void ITranslator.Translate(DependencyObject dependencyObject, string sourceCultureName, string targetCultureName)
        {
            foreach (System.Windows.Controls.Label label in dependencyObject.FindVisualChildren<System.Windows.Controls.Label>())
            {
                if (label.Content is string)
                {
                    if (!this.dependencyObjects.ContainsKey(label))
                        this.dependencyObjects.Add(label, (string)label.Content);


                    //Text 속석을 사용 할 수 없는 컨트롤도 있음
                    try
                    {
                        label.Content = ((ITranslator)this).Translate(this.dependencyObjects[label], sourceCultureName, targetCultureName);
                    }
                    catch (Exception exception)
                    {
                        DiagnosticsTool.MyTrace(exception);
                    }
                }
            }

            foreach (System.Windows.Controls.CheckBox checkBox in dependencyObject.FindVisualChildren<System.Windows.Controls.CheckBox>())
            {
                if (checkBox.Content is string)
                {
                    if (!this.dependencyObjects.ContainsKey(checkBox))
                        this.dependencyObjects.Add(checkBox, (string)checkBox.Content);


                    //Text 속석을 사용 할 수 없는 컨트롤도 있음
                    try
                    {
                        checkBox.Content = ((ITranslator)this).Translate(this.dependencyObjects[checkBox], sourceCultureName, targetCultureName);
                    }
                    catch (Exception exception)
                    {
                        DiagnosticsTool.MyTrace(exception);
                    }
                }
            }

            foreach (System.Windows.Controls.Button button in dependencyObject.FindVisualChildren<System.Windows.Controls.Button>())
            {
                if (button.Content is string)
                {
                    if (!this.dependencyObjects.ContainsKey(button))
                        this.dependencyObjects.Add(button, (string)button.Content);


                    //Text 속석을 사용 할 수 없는 컨트롤도 있음
                    try
                    {
                        button.Content = ((ITranslator)this).Translate(this.dependencyObjects[button], sourceCultureName, targetCultureName);
                    }
                    catch (Exception exception)
                    {
                        DiagnosticsTool.MyTrace(exception);
                    }
                }
            }
        }

        void ITranslator.Restoration(DependencyObject dependencyObject)
        {
            foreach (System.Windows.Controls.Label label in dependencyObject.FindVisualChildren<System.Windows.Controls.Label>())
            {
                if (label.Content is string)
                {
                    if (this.dependencyObjects.ContainsKey(label))
                    {
                        //Text 속석을 사용 할 수 없는 컨트롤도 있음
                        try
                        {
                            label.Content = this.dependencyObjects[label];
                        }
                        catch (Exception exception)
                        {
                            DiagnosticsTool.MyTrace(exception);
                        }

                        this.dependencyObjects.Remove(label);
                    }
                }
            }

            foreach (System.Windows.Controls.CheckBox checkBox in dependencyObject.FindVisualChildren<System.Windows.Controls.CheckBox>())
            {
                if (checkBox.Content is string)
                {
                    if (this.dependencyObjects.ContainsKey(checkBox))
                    {
                        //Text 속석을 사용 할 수 없는 컨트롤도 있음
                        try
                        {
                            checkBox.Content = this.dependencyObjects[checkBox];
                        }
                        catch (Exception exception)
                        {
                            DiagnosticsTool.MyTrace(exception);
                        }

                        this.dependencyObjects.Remove(checkBox);
                    }
                }
            }

            foreach (System.Windows.Controls.Button button in dependencyObject.FindVisualChildren<System.Windows.Controls.Button>())
            {
                if (button.Content is string)
                {
                    if (this.dependencyObjects.ContainsKey(button))
                    {
                        //Text 속석을 사용 할 수 없는 컨트롤도 있음
                        try
                        {
                            button.Content = this.dependencyObjects[button];
                        }
                        catch (Exception exception)
                        {
                            DiagnosticsTool.MyTrace(exception);
                        }

                        this.dependencyObjects.Remove(button);
                    }
                }
            }
        }
    }
}