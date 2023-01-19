using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace UbloxTester
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static SerialPort? _serialPort;

        private bool _atMode = true;
        private readonly Queue<string> _initializationCommandQueue = new();

        public ObservableCollection<LogEntry> LogEntries { get; set; }

        public MainWindow()
        {
            InitializeComponent();

            var ports = SerialPort.GetPortNames().ToList();
            if (ports.Contains("COM3"))
                ports.Remove("COM3");

            foreach (var port in ports)
                ComboBoxPorts.Items.Add(port);
            if (ports.Count > 0)
                ComboBoxPorts.SelectedIndex = 0;

            DataContext = LogEntries = new ObservableCollection<LogEntry>();
        }

        private void OpenComPort(string port)
        {
            _serialPort = new SerialPort(port, 115200, Parity.None, 8, StopBits.One);
            _serialPort.Handshake = Handshake.RequestToSend;
            _serialPort.ReadTimeout = 500;
            _serialPort.DataReceived += (sender, eventArgs) =>
            {
                byte[] data = new byte[_serialPort.BytesToRead];
                _serialPort.Read(data, 0, data.Length);
                ProcessData(data);
            };

            _serialPort.Open();

            OnPortOpened();
        }

        private void OnPortOpened()
        {
            WriteEscapeFromDataMode();

            UConnectAtCommands.CreateInitializationQueue(_initializationCommandQueue);
            WriteLine(_initializationCommandQueue.Dequeue());
        }

        private void ProcessData(byte[] data)
        {
            if (_atMode)
            {
                var response = Encoding.UTF8.GetString(data);
                AddLogLine($"{response.Replace("\r\n\r\n", "\r\n")}");

                var parts = response.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 0)
                {
                    var command = parts[0];

                    
                    if (command.StartsWith("+"))
                    {
                        // split unsolicited to remove data
                        command = command.Split(":")[0];
                    }

                    switch (command)
                    {
                        case UConnectAtCommands.AtUmsm:
                        {
                            if (parts.Length == 2 && parts[1] == UConnectAtCommands.ResponseOk)
                                Console.WriteLine("Startup Mode set");
                            break;
                        }
                        case UConnectAtCommands.AtGmm:
                        {
                            var name = UConnectAtCommands.ParseResponseForString(parts);
                            if (name != null)
                                UpdateTextBlock(TextBlockName, name);
                            break;
                        }
                        case UConnectAtCommands.AtGmi:
                        {
                            var manufacturer = UConnectAtCommands.ParseResponseForString(parts);
                            if (manufacturer != null)
                                Console.WriteLine("Got manufacturer: " + manufacturer);
                            break;
                        }
                        case UConnectAtCommands.AtGmr:
                        {
                            var version = UConnectAtCommands.ParseResponseForString(parts);
                            if (version != null)
                                UpdateTextBlock(TextBlockVersion, version);
                            break;
                        }
                        case UConnectAtCommands.AtUmla:
                        {
                            var address = UConnectAtCommands.ParseResponseForCommandPrefixedString(parts);
                            if (address != null && address.Length == 12)
                            {
                                address = FormatToMac(address);
                                UpdateTextBlock(TextBlockMac, address);
                            }
                            break;
                        }
                        case UConnectAtCommands.AtUbtleList:
                        {
                            if (parts.Length == 2)
                            {
                                if (parts[1] == UConnectAtCommands.ResponseOk)
                                {
                                    UpdateTextBlock(TextBlockConnection, "None");
                                    UpdateTextBlock(TextBlockMtu, "");
                                    UpdateTextBlock(TextBlockDataLengthExtension, "");
                                }
                            }
                            else if (parts.Length == 3)
                            {
                                if (parts[2] == UConnectAtCommands.ResponseOk)
                                {
                                    var address = UConnectAtCommands.ParseResponseForCommandPrefixedString(parts);
                                    address = address.Replace("0,", "");
                                    address = address.Replace("r", "");
                                    address = FormatToMac(address);
                                    UpdateTextBlock(TextBlockConnection, address);

                                    WriteLine(UConnectAtCommands.AtUbtleStat);
                                }
                            }
                            break;
                        }
                        case UConnectAtCommands.AtUbtleStat:
                        {
                            if (parts.Length >= 3)
                            {
                                if (parts[^1] == UConnectAtCommands.ResponseOk)
                                {
                                    for (int i = 1; i < parts.Length - 2; i++)
                                    {
                                        var pair = parts[i].Split(":")[1];
                                        var keyValuePair = pair.Split(",");
                                        if (keyValuePair.Length == 2)
                                        {
                                            var key = keyValuePair[0];
                                            switch (key)
                                            {
                                                case "1":
                                                    break;
                                                case "2":
                                                    break;
                                                case "3":
                                                    break;
                                                case "4":
                                                    UpdateTextBlock(TextBlockMtu, keyValuePair[1]);
                                                    break;
                                                case "5":
                                                    break;
                                                case "6":
                                                    UpdateTextBlock(TextBlockDataLengthExtension, keyValuePair[1] == "1" ? "True" : "False");
                                                    break;
                                            }
                                        }
                                    }
                                }
                            }
                            break;
                        }
                        case UConnectAtCommands.Ato1:
                        {
                            if (parts.Length == 2)
                            {
                                if (parts[1] == UConnectAtCommands.ResponseOk)
                                {
                                    _atMode = false;
                                }
                            }
                            break;
                        }


                        // Unsolicited

                        case UConnectAtCommands.PeerConnected:
                        case UConnectAtCommands.PeerDisconnected:
                        {
                            WriteLine(UConnectAtCommands.AtUbtleList);
                            break;
                        }
                    }
                }

                if (_initializationCommandQueue.Count > 0)
                {
                    WriteLine(_initializationCommandQueue.Dequeue());
                }
            }
            else
            {
                AddLogLine($"Serial Data - Length:{data.Length}");
                // echo
                WriteData(data);
            }
        }

        private static string FormatToMac(string address)
        {
            string pattern = "^([0-9A-F]{2})([0-9A-F]{2})([0-9A-F]{2})([0-9A-F]{2})([0-9A-F]{2})([0-9A-F]{2})$";
            string replacement = "$1:$2:$3:$4:$5:$6";
            address = Regex.Replace(address, pattern, replacement);
            return address;
        }

        public bool WriteLine(string line)
        {
            AddLogLine("> " + line);
            _serialPort?.WriteLine(line + "\r"); // sends \r\n
            return true;
        }

        public bool WriteData(byte[] data) 
        {
            _serialPort?.Write(data, 0, data.Length);
            return true;
        }

        private void WriteEscapeFromDataMode()
        {
            Thread.Sleep(1100);
            WriteData(Encoding.UTF8.GetBytes("+++"));
            Thread.Sleep(1100);
        }

        private void AddLogLine(string line)
        {
            Dispatcher.BeginInvoke((Action)(() => LogEntries.Add(new LogEntry() { Message = line })));
        }

        private bool AutoScroll = true;
        private void ScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (e.Source is not ScrollViewer scrollViewer) return;

            if (e.ExtentHeightChange == 0)
            {
                AutoScroll = Math.Abs(scrollViewer.VerticalOffset - scrollViewer.ScrollableHeight) < 0.1;
            }
            
            if (AutoScroll && e.ExtentHeightChange != 0)
            {   
                scrollViewer.ScrollToVerticalOffset(scrollViewer.ExtentHeight);
            }
        }

        private void UpdateTextBlock(TextBlock textBlock, string text)
        {
            Dispatcher.BeginInvoke((Action)(() => textBlock.Text = text));
        }

        private bool _isOpen = false;

        private void ButtonOpen_OnClick(object sender, RoutedEventArgs e)
        {
            if (!_isOpen)
            {
                var port = ComboBoxPorts.Text;
                OpenComPort(port);
                ButtonDataMode.IsEnabled = true;
                ButtonStatus.IsEnabled = true;
            }
            else
            {
                _serialPort?.Close();
                ButtonAtMode.IsEnabled = false;
                ButtonDataMode.IsEnabled = false;
                ButtonStatus.IsEnabled = false;
            }
            _isOpen = !_isOpen;
            ButtonOpen.Content = _isOpen ? "Close" : "Open";
        }

        private void ButtonAtMode_OnClick(object sender, RoutedEventArgs e)
        {
            Cursor = Cursors.AppStarting; // this will hang so show cursor
            WriteEscapeFromDataMode();
            _atMode = true;
            ButtonAtMode.IsEnabled = false;
            ButtonDataMode.IsEnabled = true;
            ButtonStatus.IsEnabled = true;
            Cursor = Cursors.Arrow;
        }

        private void ButtonDataMode_OnClick(object sender, RoutedEventArgs e)
        {
            WriteLine(UConnectAtCommands.Ato1);
            ButtonAtMode.IsEnabled = true;
            ButtonDataMode.IsEnabled = false;
            ButtonStatus.IsEnabled = false;
        }

        private void ButtonRequestStatus_OnClick(object sender, RoutedEventArgs e)
        {
            WriteLine(UConnectAtCommands.AtUbtleList);
        }
    }
}
