﻿using HandyControl.Controls;
using HP_Driver_Tool.Utilities;
using SimpleWifi;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;

namespace HP_Driver_Tool.Models
{
    internal class WifiConnaction : Singleton<WifiConnaction>
	{
		private Wifi m_wifi;

		public WifiConnaction()
		{
			m_wifi = new Wifi();
			if (m_wifi.NoWifiAvailable)
				Console.WriteLine("\r\n-- NO WIFI CARD WAS FOUND --");
		}
		public void Disconnect()
		{
			m_wifi.Disconnect();
		}

		public void Status()
		{
			Console.WriteLine("\r\n-- CONNECTION STATUS --");
			if (m_wifi.ConnectionStatus == WifiStatus.Connected)
				Console.WriteLine("You are connected to a wifi");
			else
				Console.WriteLine("You are not connected to a wifi");
		}

		public IEnumerable<AccessPoint> List()
		{
			bool loop;
            do { 
				loop = false;
				try
				{
					return m_wifi.GetAccessPoints().OrderByDescending(ap => ap.SignalStrength);
				}
				catch (Win32Exception)
				{
					HandyControl.Controls.MessageBox.Show("A vezeték nélküli helyi hálózat adaptere ki van kapcsolva, és nem támogatja a kért műveletet", "Hálózati hiba", MessageBoxButton.OK);
					loop = true;
				}
			} while (loop);
			return Enumerable.Empty<AccessPoint>();
		}


		public bool Connect(string ssid, bool overwrite = false)
		{
			var accessPoints = List();

			AccessPoint selectedAP = accessPoints.FirstOrDefault(a => a.Name == ssid);

			if (selectedAP != default)
			{
				// Auth
				AuthRequest authRequest = new AuthRequest(selectedAP);

				if (authRequest.IsPasswordRequired)
				{
					if (selectedAP.HasProfile)
					// If there already is a stored profile for the network, we can either use it or overwrite it with a new password.
					{
						Console.Write("\r\nA network profile already exist, do you want to use it (y/n)? ");
						if (Console.ReadLine().ToLower() == "y")
						{
							overwrite = false;
						}
					}

					if (overwrite)
					{
						if (authRequest.IsUsernameRequired)
						{
							Console.Write("\r\nPlease enter a username: ");
							authRequest.Username = Console.ReadLine();
						}

						authRequest.Password = PasswordPrompt(selectedAP);

						if (authRequest.IsDomainSupported)
						{
							Console.Write("\r\nPlease enter a domain: ");
							authRequest.Domain = Console.ReadLine();
						}
					}
				}

				return selectedAP.Connect(authRequest, overwrite);
			}
			return false;
		}

		private string PasswordPrompt(AccessPoint selectedAP)
		{
			string password = string.Empty;

			bool validPassFormat = false;

			while (!validPassFormat)
			{
				Console.Write("\r\nPlease enter the wifi password: ");
				password = Console.ReadLine();

				validPassFormat = selectedAP.IsValidPassword(password);

				if (!validPassFormat)
					Console.WriteLine("\r\nPassword is not valid for this network type.");
			}

			return password;
		}

		public void ProfileXML()
		{
			var accessPoints = List();

			Console.Write("\r\nEnter the index of the network you wish to print XML for: ");

			int selectedIndex = int.Parse(Console.ReadLine());
			if (selectedIndex > accessPoints.ToArray().Length || accessPoints.ToArray().Length == 0)
			{
				Console.Write("\r\nIndex out of bounds");
				return;
			}
			AccessPoint selectedAP = accessPoints.ToList()[selectedIndex];

			Console.WriteLine("\r\n{0}\r\n", selectedAP.GetProfileXML());
		}

		public void DeleteProfile()
		{
			var accessPoints = List();

			Console.Write("\r\nEnter the index of the network you wish to delete the profile: ");

			int selectedIndex = int.Parse(Console.ReadLine());
			if (selectedIndex > accessPoints.ToArray().Length || accessPoints.ToArray().Length == 0)
			{
				Console.Write("\r\nIndex out of bounds");
				return;
			}
			AccessPoint selectedAP = accessPoints.ToList()[selectedIndex];

			selectedAP.DeleteProfile();
			Console.WriteLine("\r\nDeleted profile for: {0}\r\n", selectedAP.Name);
		}


		public void ShowInfo()
		{
			var accessPoints = List();

			Console.Write("\r\nEnter the index of the network you wish to see info about: ");

			int selectedIndex = int.Parse(Console.ReadLine());
			if (selectedIndex > accessPoints.ToArray().Length || accessPoints.ToArray().Length == 0)
			{
				Console.Write("\r\nIndex out of bounds");
				return;
			}
			AccessPoint selectedAP = accessPoints.ToList()[selectedIndex];

			Console.WriteLine("\r\n{0}\r\n", selectedAP.ToString());
		}

		private void wifi_ConnectionStatusChanged(object sender, WifiStatusEventArgs e)
		{
			Console.WriteLine("\nNew status: {0}", e.NewStatus.ToString());
		}

		private void OnConnectedComplete(bool success)
		{
			Console.WriteLine("\nOnConnectedComplete, success: {0}", success);
		}
	}
}