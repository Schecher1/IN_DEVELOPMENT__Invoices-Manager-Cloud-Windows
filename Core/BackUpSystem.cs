﻿using InvoicesManager.Classes;
using InvoicesManager.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;

namespace InvoicesManager.Core
{
    public class BackUpSystem
    {
        private static MainWindow _mainWindow;

        public static bool BackUp(string backupFilePath, MainWindow mainWindow)
        {
            //set the main window for the progress bar
            _mainWindow = mainWindow;

            //refresh all invoices
            InvoiceSystem.Init();
            
            bool WasPerformedCorrectly = false;
            BackUpModel backUp = new BackUpModel();
            List<InvoiceModel> allInvoices = EnvironmentsVariable.allInvoices;
            List<InvoiceBackUpModel> invoices = new List<InvoiceBackUpModel>();

            //clear the progress bar
            _mainWindow.ClearInfoProgressBar();

            //set the progress bar max value 
            //2 => SerializeObject & WriteAllText 
            _mainWindow.SetInfoProgressMaxValue(allInvoices.Count + 2);

            //create all InvoiceBackUpModel and add them into the list
            foreach (InvoiceModel invoice in allInvoices)
            {
                InvoiceBackUpModel tmpBackUp = new InvoiceBackUpModel()
                {
                    //Base64 = Convert.ToBase64String(File.ReadAllBytes(invoice.Path)),
                    Invoice = new SubInvoiceBackUpModel()
                    {
                        ExhibitionDate = invoice.ExhibitionDate,
                        Organization = invoice.Organization,
                        DocumentType = invoice.DocumentType,
                        InvoiceNumber = invoice.InvoiceNumber,
                        Reference = invoice.Reference
                    }
                };

                invoices.Add(tmpBackUp);
                _mainWindow.SetInfoProgressBarValue(1);
            }

            //set the creation date
            backUp.DateOfCreation = DateTime.Now;

            //set the entity count
            backUp.EntityCount = invoices.Count;

            //link the invoices list to the backUp
            backUp.Invoices = invoices;

            //serialize the backup into the json object
            string json = JsonConvert.SerializeObject(backUp);
            _mainWindow.SetInfoProgressBarValue(1);

            //write the json into the file
            File.WriteAllText(backupFilePath, json);
            _mainWindow.SetInfoProgressBarValue(1);

            //check if the file was created
            if (File.Exists(backupFilePath))
                WasPerformedCorrectly = true;

            
            if (String.IsNullOrEmpty(json))
                WasPerformedCorrectly = false;

            //clear the progress bar
            _mainWindow.ClearInfoProgressBar();

            return WasPerformedCorrectly;
        }

        public static bool Restore(string backupFilePath, MainWindow mainWindow)
        {
            //set the main window for the progress bar
            _mainWindow = mainWindow;

            bool WasPerformedCorrectly = true;
            int alreadyExistCounter = 0;
            List<string> allTempFiles = new List<string>();
            BackUpModel backUp = null;

            try
            {
                //get BackUp from the file
                backUp = JsonConvert.DeserializeObject<BackUpModel>(File.ReadAllText(backupFilePath));
            }
            catch
            {
                WasPerformedCorrectly = false;
            }

            //clear the progress bar
            _mainWindow.ClearInfoProgressBar();

            //set the progress bar max value 
            _mainWindow.SetInfoProgressMaxValue(backUp.EntityCount);

            //check if the backup is valid
            if (!WasPerformedCorrectly)
                return false;

            //check if the file was read
            if (backUp == null)
                return false;

            //check if the file is empty
            if (backUp.EntityCount == 0)
            {
                MessageBox.Show("The backup is empty!", "Error", MessageBoxButton.OK);
                return false;
            }

            //check if the file is corrupted
            if (backUp.Invoices == null)
                return false;

            //go through all sub models, 
            //then decode the file from the 64base
            //check if the file exists and if not create it
            //then create the invoice
            //and finally add it to the system

            try
            {
                foreach (InvoiceBackUpModel invoice in backUp.Invoices)
                {
                    byte[] bytes = Convert.FromBase64String(invoice.Base64);
                    String hashCode = HashManager.GetMD5HashFromByteArray(bytes);
                    string path = Path.Combine(Path.GetTempPath(), hashCode + ".pdf");

                    File.WriteAllBytes(path, bytes);

                    string hashID = HashManager.GetMD5HashFromFile(path);
                    string newPath = @$"{EnvironmentsVariable.PathInvoices}{hashID}.pdf";

                    //add the path, so we can delete it later (we know windows doesn't do it for us :))
                    allTempFiles.Add(path);

                    if (InvoiceSystem.CheckIfInvoiceExist(path))
                    {
                        alreadyExistCounter++;
                        continue;
                    }

                    InvoiceModel tmpInvoice = new InvoiceModel()
                    {
                        ExhibitionDate = invoice.Invoice.ExhibitionDate,
                        Organization = invoice.Invoice.Organization,
                        DocumentType = invoice.Invoice.DocumentType,
                        InvoiceNumber = invoice.Invoice.InvoiceNumber,
                        Reference = invoice.Invoice.Reference,
                       // Path = newPath
                    };

                    InvoiceSystem.AddInvoice(tmpInvoice, path, newPath);
                    _mainWindow.SetInfoProgressBarValue(1);
                }
            }
            catch
            {
                WasPerformedCorrectly = false;
            }

            //clear the progress bar
            _mainWindow.ClearInfoProgressBar();

            //delete all temp files
            foreach (string file in allTempFiles)
                try { File.Delete(file); } catch { }

            MessageBox.Show($"{backUp.EntityCount - alreadyExistCounter} {Application.Current.Resources["invoicesWereRestored"] as string}" + Environment.NewLine +
                                          $"{alreadyExistCounter} {Application.Current.Resources["invoicesWereSkipped"] as string}" + Environment.NewLine +
                                          $"{Application.Current.Resources["fromTotal"] as string} {backUp.EntityCount} {Application.Current.Resources["invoices"] as string}",
                                          Application.Current.Resources["recoveryCompleted"] as string, MessageBoxButton.OK);

            return WasPerformedCorrectly;
        }
    }
}
