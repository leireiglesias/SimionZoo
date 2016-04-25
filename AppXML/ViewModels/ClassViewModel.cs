﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Caliburn.Micro;
using AppXML.Models;
using System.Collections.ObjectModel;
using System.Xml;
using System.Windows.Controls;
using AppXML.Data;
using System.Dynamic;
using System.Windows;

namespace AppXML.ViewModels
{
    public class MultibuttonInfo
    {
        
        public string content { get; set;}
        public string visible { get; set;}
        public enum MultiType
        {
            
            header,
            added
        };
        public MultiType type { get; set; }
        public MultibuttonInfo()
        {
            visible = "Hidden";
        }
    }
    public class ClassViewModel:PropertyChangedBase
    {
        public string Border { get; set; }
        public string Margin { get; set; }
        private MultiValuedViewModel _father;
        private ClassViewModel _resumeClassViewModel;
        public MultibuttonInfo MultiButton { get; set; }
        private readonly ClassViewModel owner;
        public ClassViewModel ResumeClass { get { return _resumeClassViewModel; } set { _resumeClassViewModel = value; NotifyOfPropertyChange(() => ResumeClass); } }
        private ObservableCollection<ValidableAndNodeViewModel> _allItems = new ObservableCollection<ValidableAndNodeViewModel>();
       
        private ChoiceViewModel _choice;
        private string _buttonColor;
        public string ButtonColor { get { return _buttonColor; } set { _buttonColor = value; NotifyOfPropertyChange(() => ButtonColor); } }
        private string _itemName;
        private string _className;
        private WindowClassViewModel _wclvm;
        private XmlDocument _doc;
        public XmlNode resume;

        public void MultiAction()
        {
            if(_father!=null && MultiButton!=null)
            {
                if (MultiButton.type == MultibuttonInfo.MultiType.header)
                    _father.Add();
                else
                    _father.DeleteClass(this);
            }
        }

        public void setResumeInClassView()
        {
            _wclvm.Save();
        }
        public ClassViewModel(string clasName,string itemName, Boolean ignoreWindow,XmlDocument doc, ClassViewModel owner)
        {
            this.MultiButton = new MultibuttonInfo();
            this.Border = "0";
            this.Margin = "0";
            _doc = doc;
            this.owner = owner;
            _className = clasName;
            XmlNode node = CNode.definitions[clasName];
            if (ignoreWindow&&node.Attributes["Window"] != null)
            {

                this._itemName = itemName;
                _wclvm = new WindowClassViewModel(clasName, this, _doc);
                //CApp.addNewClass(this);

            }
            else
            {
                construc(node);
            }
        }
        public void setMultiButtonInfo(AppXML.ViewModels.MultibuttonInfo.MultiType type,MultiValuedViewModel mvvm)
        {
            this.Border = "1";
            this.Margin = "5";
            this._father = mvvm;
            this.MultiButton.type = type;
            this.MultiButton.visible = "Visible";
            if(type==AppXML.ViewModels.MultibuttonInfo.MultiType.header)
            {
                MultiButton.content = "Add";
            }
            else
            {
                MultiButton.content = "Remove";
            }

        }
        public ClassViewModel(string clasName,string itemName,  XmlDocument doc)
        {
            this.MultiButton = new MultibuttonInfo();
            this.Border = "0";
            this.Margin = "0";
            _doc = doc;
            _className = clasName;
            XmlNode node = CNode.definitions[clasName];
            if (node.Attributes["Window"] != null)
            {

                this._itemName = itemName;
                _wclvm = new WindowClassViewModel(clasName, this,_doc);
               
            }
            else
            {
                construc(node);
            }


        }
        private void construc(XmlNode node)
        {
            foreach (XmlNode child in node.ChildNodes)
            {
                if (child.Name == "CHOICE")
                {
                    string tag = null;
                    if (child.Attributes["XMLTag"] != null)
                        tag = child.Attributes["XMLTag"].Value;
                    _choice = new ChoiceViewModel(child, _doc, tag);
                    _allItems.Add(_choice);
                }
                else if (child.Name.EndsWith("VALUE"))
                {
                    //if (_items == null)
                    //  _items = new ObservableCollection<IntegerViewModel>();
                    CIntegerValue civ = CNode.getInstance(child) as CIntegerValue;
                    string tag = null;
                    if (child.Attributes["XMLTag"] != null)
                        tag = child.Attributes["XMLTag"].Value;
                    IntegerViewModel ivw = new IntegerViewModel(child.Attributes["Name"].Value, civ, _doc, tag);
                    // IntegerViewModel ivw = new IntegerViewModel(child.Attributes["Name"].Value, civ,_doc);
                    _allItems.Add(ivw);

                }
                else if (child.Name == "MULTI-VALUED")
                {
                    //to do: añadir los multis a su lista y añadirlo en el xaml
                    //if (_multis == null)
                    //  _multis = new ObservableCollection<MultiValuedViewModel>();
                    string comment = null;
                    if (child.Attributes["Comment"] != null)
                        comment = child.Attributes["Comment"].Value;
                    string tag = null;
                    if (child.Attributes["XMLTag"] != null)
                        tag = child.Attributes["XMLTag"].Value;
                    string def = null;
                    if (child.Attributes["Default"] != null)
                    {
                        def = child.Attributes["Default"].Value;
                    }
                    if (!CNode.definitions.ContainsKey(child.Attributes["Class"].Value))
                    {
                        MultiSimpleViewModel simple = new MultiSimpleViewModel(child.Attributes["Name"].Value, child.Attributes["Class"].Value, def, comment, _doc, tag);
                        _allItems.Add(simple);
                    }
                    else
                    {
                        MultiValuedViewModel mvvm = new MultiValuedViewModel(child.Attributes["Name"].Value, child.Attributes["Class"].Value, comment, _doc, tag);
                        _allItems.Add(mvvm);
                    }
                    //MultiValuedViewModel mvvm = new MultiValuedViewModel(child.Attributes["Name"].Value, child.Attributes["Class"].Value, comment, isOptional, doc, tag);
                    //_multis.Add(mvvm);
                }
                else if (child.Name == "BRANCH")
                {

                    //if (_branches == null)
                    //    _branches = new ObservableCollection<BranchViewModel>();
                    bool isOptional = false;
                    if (child.Attributes["Optional"] != null)
                        isOptional = Convert.ToBoolean(child.Attributes["Optional"].Value);
                    string comment = null;
                    if (child.Attributes["Comment"] != null)
                        comment = child.Attributes["Comment"].Value;
                    string tag = null;
                    if (child.Attributes["XMLTag"] != null)
                        tag = child.Attributes["XMLTag"].Value;
                    BranchViewModel bvm = new BranchViewModel(child.Attributes["Name"].Value, child.Attributes["Class"].Value, comment, isOptional, _doc, tag);
                    _allItems.Add(bvm);
                }
                else if (child.Name == "XML-NODE-REF")
                {
                    string label = child.Attributes["Name"].Value;
                    string action = child.Attributes["HangingFrom"].Value;
                    string xmlfile = child.Attributes["XMLFile"].Value;
                    //if (_XMLNODE == null)
                    //   _XMLNODE = new ObservableCollection<XMLNodeRefViewModel>();
                    string tag = null;
                    if (child.Attributes["XMLTag"] != null)
                        tag = child.Attributes["XMLTag"].Value;
                    _allItems.Add(new XMLNodeRefViewModel(label, xmlfile, action, _doc, tag, this));
                }
                else if (child.Name == "RESUME")
                {
                    resume = child;

                }
            }
        }
        public ClassViewModel(string clasName, string itemName,Boolean ignoreWindow, XmlDocument doc)
        {
            _doc = doc;
            _className = clasName;
            XmlNode node = CNode.definitions[clasName];
            if (ignoreWindow && node.Attributes["Window"] != null)
            {
                _itemName = itemName;
                _wclvm = new WindowClassViewModel(clasName, this,_doc);
            }
            else
            {
                construc(node);  
            }


        }

        public string ClassViewVisible { get { if (_itemName != null)return "Hidden"; else return "Visible"; } set { } }
        public string ResumeVisible { get { if (_itemName == null)return "Hidden"; else return "Visible"; } set { } }
        //public string ItemsVisible { get { if(Items == null || _resume!=null )return "Hidden";else return "Visible"; } set { } }
        //public string ChoiceVisible { get { if (Choice == null || _resume != null)return "Hidden"; else return "Visible"; } set { } }
        //public string BranchesVisible { get { if (Branches == null || _resume != null)return "Hidden"; else return "Visible"; } set { } }
        //public string MultisVisible { get { if (Multis == null || _resume != null)return "Hidden"; else return "Visible"; } set { } }
       // public string XMLNodeVisible { get { if (_XMLNODE == null || _resume != null)return "Hidden"; else return "Visible"; } set { } }
        public ChoiceViewModel Choice { get { return _choice; } set { _choice = value; } }
        //public ObservableCollection<IntegerViewModel> Items { get { return _items; } set { } }
        //public ObservableCollection<MultiValuedViewModel> Multis { get { return _multis; } set { } }
        //public ObservableCollection<BranchViewModel> Branches { get { return _branches; } set { } }
        //public ObservableCollection<XMLNodeRefViewModel> XMLNODE { get { return _XMLNODE; } set { } }
        public ObservableCollection<ValidableAndNodeViewModel> AllItems { get { return _allItems; } set { } }
        public string ItemName { get { return _itemName; } set { _itemName = value; NotifyOfPropertyChange(() => ItemName); } }
        public void Copy()
        {
            if (this.validate(false))
            {
                XmlNode data = getXmlNode();
                if (data != null)
                {
                    System.Windows.Clipboard.SetText("<" + _className + ">" + data.OuterXml + "</" + _className + ">");
                }
            }
        }
        public void Paste()
        {
            string data = System.Windows.Clipboard.GetText();
            if (data != null)
                try
                {
                    XmlDocument doc = new XmlDocument();
                    doc.LoadXml(data);
                    if (doc.DocumentElement.Name.Equals(this._className))
                    {
                        this.setAsNull();
                        AppXML.Data.Utility.fillTheClass(this, doc.DocumentElement.FirstChild);
                    }
                    else
                    {

                    }

                }
                catch
                {

                }
           
        }
        public void removeViews()
        {
            foreach(ValidableAndNodeViewModel item in _allItems)
            {
                if (item is XMLNodeRefViewModel)
                {
                    CApp.removeView(item as XMLNodeRefViewModel);
                }
                if (item is BranchViewModel)
                    (item as BranchViewModel).removeViews();
                if (item is ChoiceViewModel)
                    (item as ChoiceViewModel).removeViews();
                if (item is MultiValuedViewModel)
                    (item as MultiValuedViewModel).removeViews();
                if (item is MultiXmlNodeRefViewModel)
                    (item as MultiXmlNodeRefViewModel).removeViews();
            }
            //if(_XMLNODE!=null)
              //  CApp.removeViews(_XMLNODE.ToList());
            //if (_branches != null)
           // {
             //   foreach(BranchViewModel branch in _branches)
               // {
                 //   branch.removeViews();
                //}
            //}
            //if(_choice!=null)
            //{
              //  _choice.removeViews();
           // }
            //if(_multis!=null)
           // {
             //   foreach(MultiValuedViewModel multi in _multis)
              //  {
              //      multi.removeViews();
              //  }
          //  }
                
        }
        public void OpenForm()
        {
            

           WindowManager windowManager = new WindowManager();
           dynamic settings = new ExpandoObject();
           settings.WindowStyle = WindowStyle.ThreeDBorderWindow;
           settings.ShowInTaskbar = true;
           settings.Title = this._className;
           settings.WindowState = WindowState.Normal;
            settings.ResizeMode = ResizeMode.CanMinimize;

           
           windowManager.ShowDialog(this._wclvm,null,settings);
           

            
        }
        public bool validate(bool isCallByContructor)
        {
            if(_itemName==null || _itemName=="")
            {
                foreach(ValidableAndNodeViewModel item in _allItems)
                {
                    if (!item.validate())
                    {
                        if(this.owner!=null)
                            owner.ButtonColor = "Red";
                        return false;
                    }    
                }
                if (CApp.IsInitializing)
                {
                    if (this.owner != null)
                        owner.ButtonColor = "Orange";
                }  
                else
                {
                    if (this.owner != null)
                        owner.ButtonColor = "White";
                }
               
                return true;
            }
           
            else
            {
                return ResumeClass.validate(isCallByContructor);
               
            }
            
        }

        public List<XmlNode> getXmlNodes()
        {
            
            List<XmlNode> result = new List<XmlNode>();
            //XmlNode nodo = _doc.CreateElement(_className);
            if(_itemName==null)
            {
                foreach (ValidableAndNodeViewModel item in _allItems)
                {

                    List<XmlNode> tmp = item.getXmlNode();
                    if (tmp != null)
                        result.AddRange(tmp);

                }
                /*if (_branches != null)
                {
                    foreach (BranchViewModel item in _branches)
                    {
                        XmlNode bvmn = item.getXmlNode();
                        if(bvmn!=null)
                            result.Add(item.getXmlNode());
                        //nodo.AppendChild(item.getXmlNode());
                    }
                }
                if (_items != null)
                {
                    foreach (IntegerViewModel item in _items)
                    {
                        result.Add(item.getXmlNode());
                        //nodo.AppendChild(item.getXmlNode());
                    }
                }
                if (_multis != null)
                {
                    foreach (MultiValuedViewModel item in _multis)
                    {
                        List<XmlNode> nodes = item.getXmlNode();
                        if(nodes!=null)
                        {
                            foreach (XmlNode node in nodes)
                                //nodo.AppendChild(node);
                                result.Add(node);
                        }
                        
                    }
                }

                if (_choice != null)
                {
                    //nodo.AppendChild(_choice.getXmlNode());
                    result.Add(_choice.getXmlNode());
                }
                if (_XMLNODE != null)
                {
                    foreach (XMLNodeRefViewModel item in _XMLNODE)
                    {
                        //nodo.AppendChild(item.getXmlNode());
                        result.Add(item.getXmlNode());
                    }
                }*/
              
            }
            else
            {
                return ResumeClass.getXmlNodes();
            }       
            return result;
           
        }
        public XmlNode getXmlNode()
        {
            
            XmlNode nodo = _doc.CreateElement(_className);
            List<XmlNode> list = getXmlNodes();
            foreach (XmlNode node in list)
                nodo.AppendChild(node);
            return nodo;
            /*if (_resume == null)
            {
                if (_branches != null)
                {
                    foreach (BranchViewModel item in _branches)
                    {
                        
                        nodo.AppendChild(item.getXmlNode());
                    }
                }
                if (_items != null)
                {
                    foreach (IntegerViewModel item in _items)
                    {
                        
                        nodo.AppendChild(item.getXmlNode());
                    }
                }
                if (_multis != null)
                {
                    foreach (MultiValuedViewModel item in _multis)
                    {
                        List<XmlNode> nodes = item.getXmlNode();
                        foreach (XmlNode node in nodes)
                            nodo.AppendChild(node);
                            
                    }
                }

                if (_choice != null)
                {
                    nodo.AppendChild(_choice.getXmlNode());
                    
                }
                if (_XMLNODE != null)
                {
                    foreach (XMLNodeRefViewModel item in _XMLNODE)
                    {
                        nodo.AppendChild(item.getXmlNode());
                        
                    }
                }

            }
            else
            {
                return ResumeClass.getXmlNode();
            }
            return nodo;*/
            
        }

        public void setAsNull()
        {
            foreach(ValidableAndNodeViewModel item in this._allItems)
            {
                BranchViewModel bvwm = item as BranchViewModel;
                ChoiceViewModel cvm = item as ChoiceViewModel;
                MultiValuedViewModel mvvm = item as MultiValuedViewModel;
                IntegerViewModel ivm = item as IntegerViewModel;
                if(ivm!=null && ivm.IsOptional)
                {
                    ivm.Value = "";
                }
                if (bvwm != null)
                {
                    bvwm.setAsNull();
                }
               /* if(cvm !=null)
                {
                    cvm.setAsNull();
                }*/
                if(mvvm !=null)
                {
                    mvvm.setAsNull();
                }
            }
        }

        public void removeFromNewClass()
        {
            Models.CApp.removeFromNew(this);
            foreach (ValidableAndNodeViewModel item in _allItems)
            {
                if (item is BranchViewModel)
                    (item as BranchViewModel).removeFromNew();
                if (item is ChoiceViewModel)
                    (item as ChoiceViewModel).removeViewsFromNew();
                if (item is MultiValuedViewModel)
                    (item as MultiValuedViewModel).removeViewsFromNew();
               
            }
        }
    }
}
