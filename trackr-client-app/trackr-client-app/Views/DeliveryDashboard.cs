﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using trackr_client_app.Models;

namespace trackr_client_app.Views
{
    public partial class DeliveryDashboard : Form
    {
        bool signout = false;
        public DeliveryDashboard()
        {
            InitializeComponent();
        }

        private void DeliveryDashboard_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (signout) return;
            var loginForm = (LoginForm)Tag;
            loginForm.Close();
        }

        private void searchBtn_Click(object sender, EventArgs e)
        {
            if(searchBtn.Text == null) return;
            string searchValue = searchTB.Text;
            if(tabControl1.SelectedIndex == 0)
            {
                parcelGridView.ClearSelection();
                parcelGridView.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
                try
                {
                    foreach (DataGridViewRow row in parcelGridView.Rows)
                    {
                        if (row.Cells[1].Value.ToString().Equals(searchValue))
                        {
                            row.Selected = true;
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }
            }
            else if(tabControl1.SelectedIndex == 1)
            {
                parcelProcessedGridView.ClearSelection();
                parcelProcessedGridView.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
                try
                {
                    foreach (DataGridViewRow row in parcelProcessedGridView.Rows)
                    {
                        if (row.Cells[1].Value.ToString().Equals(searchValue))
                        {
                            row.Selected = true;
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }
            }
        }

        private void DeliveryDashboard_Load(object sender, EventArgs e)
        {
            usernameLabel.Text = UserSession.delivery.ManName;
            if(UserSession.delivery.ManImage != null)
            {
                pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
                pictureBox1.LoadAsync(UserSession.delivery.ManImage);
            }
            parcelProcessedGridView.Visible = false;
            GetData();
        }

        private async void GetData()
        {
            HttpClient client = new HttpClient();

            // Lấy đơn hàng trước
            var response = await client.GetAsync(UserSession.apiUrl + "Parcel");
            var responseString = await response.Content.ReadAsStringAsync();
            var parcels = JArray.Parse(responseString);
            LoadParcelData(parcels);

            // Lấy thông tin khách hàng, do bất đồng bộ nên để chung một hàm để đảm bảo thứ tự không bị sai
            response = await client.GetAsync(UserSession.apiUrl + "Customer");
            responseString = await response.Content.ReadAsStringAsync();
            var customers = JArray.Parse(responseString);
            LoadCustomerData(customers);
            DisplayData();
            DisplayProcessedData();
        }
        private void LoadCustomerData(JArray customers)
        {
            foreach(JObject customer in customers.Cast<JObject>())
            {
                Customer newCustomer = new Customer();
                newCustomer = JsonConvert.DeserializeObject<Customer>(customer.ToString());
                UserSession.customers.Add(newCustomer);
            }
        }
        private void LoadParcelData(JArray parcels)
        {
            foreach (JObject parcel in parcels.Cast<JObject>())
            {
                Parcel newParcel = new Parcel();
                newParcel = JsonConvert.DeserializeObject<Parcel>(parcel.ToString());
                UserSession.parcels.Add(newParcel);
            }
        }
        private void DisplayData()
        {
            int i = 1;
            parcelGridView.Rows.Clear();
            foreach (Parcel parcel in UserSession.parcels)
            {
                if (parcel.ParStatus == "READY_TO_SHIP")
                {
                    Customer customer = UserSession.customers.Find(x => x.CusID == parcel.CusID);
                    parcelGridView.Rows.Add(i++, parcel.ParID.ToString(), parcel.ParDescription, parcel.ParDeliveryDate.ToString(), customer.CusAddress);
                }
            }
        }

        private void DisplayProcessedData()
        {
            int i = 1;
            parcelProcessedGridView.Rows.Clear();
            foreach (Parcel parcel in UserSession.parcels)
            {
                if ((parcel.ParStatus == "TO_CONFIRM_PAYMENT" || parcel.ParStatus == "PROCESSED") && parcel.ManID == UserSession.delivery.ManID)
                {
                    Customer customer = UserSession.customers.Find(x => x.CusID == parcel.CusID);
                    parcelProcessedGridView.Rows.Add(i++, parcel.ParID.ToString(), parcel.ParDescription, parcel.ParDeliveryDate.ToString(), customer.CusAddress);
                }
            }
        }
        private void parcelGridView_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            parcelGridView.SelectionMode = DataGridViewSelectionMode.CellSelect;
            var senderGrid = (DataGridView)sender;
            var stringValue = senderGrid.Rows[e.RowIndex].Cells[1].Value;
            int value = int.Parse(stringValue.ToString());
            if (senderGrid.Columns[e.ColumnIndex] is DataGridViewButtonColumn && e.RowIndex >= 0)
            {
                Parcel parcel = UserSession.parcels.Find(x => x.ParID == value);
                DeliveryConfirmView deliveryConfirmView = new DeliveryConfirmView(parcel);
                deliveryConfirmView.StartPosition = FormStartPosition.CenterScreen;
                deliveryConfirmView.Tag = this;
                deliveryConfirmView.Show();
            }
        }

        public void RefreshData()
        {
            UserSession.parcels.Clear();
            UserSession.customers.Clear();
            parcelGridView.Rows.Clear();
            parcelGridView.Refresh();
            parcelProcessedGridView.Rows.Clear();
            parcelProcessedGridView.Refresh();
            GetData();
        }

        private void refreshBtn_Click(object sender, EventArgs e)
        {
            RefreshData();
        }

        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (tabControl1.SelectedIndex)
            {
                case 0:
                    {
                        parcelProcessedGridView.Visible = false;
                        parcelGridView.Visible = true;
                        DisplayData(); break;
                    }
                case 1:
                    {
                        parcelProcessedGridView.Visible = true;
                        parcelGridView.Visible = false;
                        DisplayProcessedData(); break;
                    }
            }
        }

        private void parcelProcessedGridView_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            parcelProcessedGridView.SelectionMode = DataGridViewSelectionMode.CellSelect;
            var senderGrid = (DataGridView)sender;
            var stringValue = senderGrid.Rows[e.RowIndex].Cells[1].Value;
            int value = int.Parse(stringValue.ToString());
            if (senderGrid.Columns[e.ColumnIndex] is DataGridViewButtonColumn && e.RowIndex >= 0)
            {
                Parcel parcel = UserSession.parcels.Find(x => x.ParID == value);
                Customer customer = UserSession.customers.Find(x => x.CusID == parcel.CusID);
                DeliveryParcelView deliveryParcelView = new DeliveryParcelView(parcel, customer);
                deliveryParcelView.Show();
            }
        }

        private void logOutBtn_Click(object sender, EventArgs e)
        {
            signout = true;
            var loginForm = (LoginForm)Tag;
            loginForm.Show();
            Close();
        }
    }
}
