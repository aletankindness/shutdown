﻿using ShutDown.Data;
using ShutDown.Models;
using System;
using System.Windows.Input;
using System.Windows.Threading;

namespace ShutDown
{
    public class MainViewModel : ObservableObject
    {
        private ShutDownOperation _operation = ShutDownOperation.ShutDown;
        private ICommand _selectOperationCommand;
        private ICommand _addDelayCommand;
        private ICommand _toggleForceCommand;
        private ICommand _startShutDownCommand;
        private ICommand _cancelShutDownCommand;
        private int _delayMinutes = 60;
        private bool _shutDownInProgress;
        private string _shutDownRemainingTime;
        private string _operationName;
        private DispatcherTimer _timer;
        private DateTime _startTime;
        public event EventHandler CloseApp;

        public MainViewModel()
        {
            var settings = Settings.Instance;
            Operation = settings.DefaultOperation;
            DelayMinutes = settings.DefaultDelay;
            MinMinutes = settings.MinMinutes;
            MaxMinutes = settings.MaxMinutes;
            Force = settings.DefaultForce;
        }

        public int MinMinutes { get; private set; }
        public int MaxMinutes { get; private set; }
        public int DefaultMinutes { get; private set; }
        public bool Force { get; private set; }
        public bool ShutDownInProgress
        {
            get { return _shutDownInProgress; }
            set
            {
                if (value != _shutDownInProgress)
                {
                    _shutDownInProgress = value;
                    RaisePropertyChanged(nameof(ShutDownInProgress));
                }
            }
        }
        public string ShutDownRemainingTime
        {
            get { return _shutDownRemainingTime; }
            set
            {
                if (value != _shutDownRemainingTime)
                {
                    _shutDownRemainingTime = value;
                    RaisePropertyChanged(nameof(ShutDownRemainingTime));
                }
            }
        }
        public string OperationName
        {
            get { return _operationName; }
            set
            {
                if (value != _operationName)
                {
                    _operationName = value;
                    RaisePropertyChanged(nameof(OperationName));
                }
            }
        }

        public ShutDownOperation Operation
        {
            get { return _operation; }
            set
            {
                if (value != _operation)
                {
                    _operation = value;
                    RaisePropertyChanged(nameof(Operation));
                }
            }
        }

        public int DelayMinutes
        {
            get { return _delayMinutes; }
            set
            {
                if (value != _delayMinutes)
                {
                    _delayMinutes = value;
                    RaisePropertyChanged(nameof(DelayMinutes));
                    RaisePropertyChanged(nameof(DelayText));
                }
            }
        }

        public string DelayText
        {
            get
            {
                var ts = TimeSpan.FromMinutes(DelayMinutes);
                return $"{Math.Floor(ts.TotalHours).ToString("00")}h {ts.Minutes.ToString("00")}min";
            }
        }

        public ICommand SelectOperationCommand => _selectOperationCommand ?? (_selectOperationCommand = new Command<string>(SelectOperation));

        public ICommand AddDelayCommand => _addDelayCommand ?? (_addDelayCommand = new Command<string>(AddDelay));

        public ICommand ToggleForceCommand => _toggleForceCommand ?? (_toggleForceCommand = new Command(ToggleForce));

        public ICommand StartShutDownCommand => _startShutDownCommand ?? (_startShutDownCommand = new Command(StartShutDown));

        public ICommand CancelShutDownCommand => _cancelShutDownCommand ?? (_cancelShutDownCommand = new Command(CancelShutDown));

        private void SelectOperation(string operation)
        {
            Operation = (ShutDownOperation)Enum.Parse(typeof(ShutDownOperation), operation, true);
            Settings.Instance.DefaultOperation = Operation;
            Settings.Instance.Save();
        }

        private void AddDelay(string value)
        {
            int val = DelayMinutes + int.Parse(value);
            if (val < MinMinutes) val = MinMinutes;
            else if (val > MaxMinutes) val = MaxMinutes;
            DelayMinutes = val;
            Settings.Instance.DefaultDelay = val;
            Settings.Instance.Save();

        }

        private void ToggleForce()
        {
            Force = !Force;
            RaisePropertyChanged(nameof(Force));
            Settings.Instance.DefaultForce = Force;
            Settings.Instance.Save();
        }

        private void StartShutDown()
        {
            _startTime = DateTime.Now;
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            _timer.Tick += (s, e) =>
            {
                OnTimerTick();
            };
            _timer.Start();
            string opName = Operation.GetOperationName(Force);
            
            opName += " in:";
            OperationName = opName;
            ShutDownInProgress = true;
        }

        private void CancelShutDown()
        {
            _timer.Stop();
            ShutDownInProgress = false;
        }

        private void OnTimerTick()
        {
            try
            {
                var now = DateTime.Now;
                TimeSpan remaining = now - _startTime;
                if (remaining.TotalSeconds > DelayMinutes * 60)
                {
                    _timer.Stop();
                    ShutDownHelper.ExecuteShutDownOperation(Operation, Force);
                    RaiseCloseApp();
                }
                else
                {
                    remaining = TimeSpan.FromMinutes(DelayMinutes) - remaining;
                    ShutDownRemainingTime = $"{remaining.Hours.ToString("00")} : {remaining.Minutes.ToString("00")} : {remaining.Seconds.ToString("00")}";
                }
            }
            catch
            {

            }
        }

        private void RaiseCloseApp()
        {
            var handler = CloseApp;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }
    }
}
