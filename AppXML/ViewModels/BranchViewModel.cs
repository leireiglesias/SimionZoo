﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Caliburn.Micro;
namespace AppXML.ViewModels
{
    public class BranchViewModel:PropertyChangedBase
    {
        private bool _isOptional;
        private string _name;
        private ClassViewModel _class;
        private bool _isNull=false;
        private string _comment;


        public string Comment { get { return _comment; } set { } }
        
        public bool IsNull { get { return _isNull; } set { _isNull = value; NotifyOfPropertyChange(() => IsNull); NotifyOfPropertyChange(() => IsEnabled); } }
        public bool IsEnabled { get { return !_isNull; } set { } }

        public string IsOptionalVisible 
        { 
            get 
            {
                if (_isOptional)
                    return "Visible";
                else
                    return "Hidden";
            }
            set { }
        }
        public BranchViewModel(string name,string clas,string comment,bool isOptional)
        {
            _name = name;
            _comment = comment;
            _class = new ClassViewModel(clas);
            _isOptional = isOptional;
        }
        public string Name{get{return _name;}set{}}
        public ClassViewModel Class { get { return _class; } set { } }
        public void removeViews()
        {
            _class.removeViews();
        }
    }
}