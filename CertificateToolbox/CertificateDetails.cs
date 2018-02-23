﻿using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Windows.Forms;
using Org.BouncyCastle.Math;

namespace CertificateToolbox
{
    public partial class CertificateDetails : UserControl
    {
        public CertificateDetails Issuer { get; set; }

        public CertificateDetails()
        {
            InitializeComponent();
        }

        public CertificateDetails(int serialNumber, CertificateDetails issuer)
        {
            InitializeComponent();

            Issuer = issuer;

            serial.Text = serialNumber.ToString();
            subject.Text = "CN=" + Environment.MachineName + serialNumber;

            store_location.DataSource = Enum.GetValues(typeof(StoreLocation));
            store_name.DataSource = Enum.GetValues(typeof(StoreName));

            store_location.SelectedItem = StoreLocation.LocalMachine;
            store_name.SelectedItem = StoreName.Root;

            not_before.Value = DateTime.UtcNow.AddDays(-1);
            not_after.Value = DateTime.UtcNow.AddYears(100);

            subject_alternative_names.ReadOnly = is_ca.Checked;
            key_usages.ReadOnly = is_ca.Checked;

            ocsp_url.Text = string.Format("http://{0}:{1}/ca.ocsp", Environment.MachineName, 8080 + serialNumber);
            crl_url.Text = string.Format("http://{0}:{1}/ca.crl", Environment.MachineName, 8180 + serialNumber);
        }

        public string SubjectAlternativeNames
        {
            get { return Serialize(subject_alternative_names.Rows); }
        }

        public string KeyUsages
        {
            get { return Serialize(key_usages.Rows);  }
        }

        private string Serialize(DataGridViewRowCollection rows)
        {
            var items = (from DataGridViewRow row in rows where row.Cells[0].Value != null select row.Cells[0].Value.ToString()).ToList();
            return items.Any() ? string.Join("#", items) : null;
        }

        public X509Certificate2 Generate()
        {
            thumbprint.Text = string.Empty;
            Refresh();

            var generator = new Generator
            {
                SerialNumber = new BigInteger(serial.Text),
                SubjectName = subject.Text,
                NotBefore = not_before.Value,
                NotAfter = not_after.Value,
                IsCertificateAuthority = is_ca.Checked,
                Issuer = Issuer?.Generate(),
                SubjectAlternativeNames = SubjectAlternativeNames?.Split('#'),
                Usages = KeyUsages?.Split('#'),
                OcspEndpoint = include_ocsp.Checked?ocsp_url.Text:null,
                CrlEndpoint = include_crl.Checked ? crl_url.Text : null,
            };

            var certificate = generator.Generate();

            if (install_store.Checked)
            {
                var store = new X509Store((StoreName)store_name.SelectedItem, (StoreLocation)store_location.SelectedItem);
                store.Open(OpenFlags.ReadWrite);
                store.Add(certificate);
                store.Close();
            }

            thumbprint.Text = certificate.Thumbprint;
            Refresh();

            return certificate;
        }

        private void is_ca_CheckedChanged(object sender, EventArgs e)
        {
            subject_alternative_names.ReadOnly = is_ca.Checked;
            key_usages.ReadOnly = is_ca.Checked;

            if (is_ca.Checked)
            {
                subject_alternative_names.Rows.Clear();
                key_usages.Rows.Clear();
            }
        }
    }
}