using HtmlAgilityPack;
using Ionic.Zip;
using Spire.Pdf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using AutoUpdaterDotNET;

namespace STJAcordao
{
    public partial class Form1 : Form
    {
        TextProgressBar tpb;
        ExTextBox firstStepETB;
        ExTextBox secondStepETB;
        static string DiarioDisponivel;
        int totalZip;
        int iZip;
        int qtdZip;
        public Form1()
        {
            InitializeComponent();
            var currentDirectory = new DirectoryInfo(Environment.CurrentDirectory);
            if (currentDirectory != null)
            {
                AutoUpdater.InstallationPath = currentDirectory.FullName;
            }
            AutoUpdater.Start("https://lucasvor.github.io/STJ-Acordao/Files/autoUpdate.xml");

            tpb = new TextProgressBar();
            tpb.Dock = DockStyle.Fill;
            tpb.VisualMode = ProgressBarDisplayMode.TextAndPercentage;
            tpb.CustomText = "Informe a data";
            panel1.Controls.Add(tpb);

            //firstStepETB = new ExTextBox();
            //firstStepETB.Dock = DockStyle.Fill;
            //firstStepETB.Hint = "Primeira parte";
            //secondStepETB = new ExTextBox();
            //secondStepETB.Dock = DockStyle.Fill;
            //secondStepETB.Hint = "Segunda parte";
            //tableLayoutPanel1.Controls.Add(firstStepETB, 0, 0);
            //tableLayoutPanel1.Controls.Add(secondStepETB, 1, 0);

        }

        private async void button1_Click(object sender, EventArgs e)
        {
            button1.Enabled = false;
            
            List<string> rr = default;
            try
            {
                DiarioDisponivel = null;
                label1.ForeColor = Color.Black;
                label2.ForeColor = Color.Black;
                rr = await GetSiteSTJNum(dateTimePicker1.Value);
                
                if (rr == null)
                {
                    throw new Exception("Não existem ocorrências nesse dia.");
                }
                if (DiarioDisponivel == null)
                {
                    throw new Exception("Não foi possivel pegar a data do diário.");
                }



                var DiarioDt = DateTime.ParseExact(DiarioDisponivel, "dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture);
                if (!File.Exists($"stj_dje_{ DiarioDt.ToString("yyyyMMdd")}.zip"))
                {
                    using (WebClient wc = new WebClient())
                    {
                        wc.DownloadProgressChanged += Wc_DownloadProgressChanged;
                        await wc.DownloadFileTaskAsync(new Uri($"https://ww2.stj.jus.br/docs_internet/processo/dje/zip/stj_dje_{DiarioDt.ToString("yyyyMMdd")}.zip"), $"stj_dje_{DiarioDt.ToString("yyyyMMdd")}.zip");

                    }
                    if (firstStepETB.Text == null || secondStepETB.Text == null)
                    {
                        throw new Exception("Campos de pesquisa não podem ser vazios.");
                    }
                }

                //ZipFile.ExtractToDirectory("test.zip", "PDF");
                await Task.Run(() =>
                 {
                     using (ZipFile zip = new ZipFile($"stj_dje_{DiarioDt.ToString("yyyyMMdd")}.zip"))
                     {
                         if (!Directory.Exists("PDF"))
                         {
                             Directory.CreateDirectory("PDF");
                         }
                         foreach (var file in new DirectoryInfo("PDF").EnumerateFiles())
                         {
                             file.Delete();
                         }
                         totalZip = iZip = 0;
                         zip.ExtractProgress += Zip_ExtractProgress;
                         zip.ExtractAll("PDF", ExtractExistingFileAction.InvokeExtractProgressEvent);

                         updateProgressBar(tpb, 100, "Processo de Extração finalizado.");

                     }
                     // Merge PDF
                     updateProgressBar(tpb, 0, "Processo de Merge Iniciado");
                     var pdfMerges = new List<string>();
                     var files = new DirectoryInfo("PDF").EnumerateFiles();
                     int iMerge = default;

                     var intersect = files.ToList().Where(x => rr.Contains(Path.GetFileNameWithoutExtension(x.FullName).Split('_')[4]));

                     foreach(var file in intersect)
                     {
                         pdfMerges.Add(file.FullName);
                         updateProgressBar(tpb, Convert.ToInt32(iMerge++ / (0.01 * intersect.Count())), $"Jutando as informações {file.Name}");
                     }
                     updateProgressBar(tpb, 0, $"Processando...");
                     using (PdfDocumentBase doc = PdfDocument.MergeFiles(pdfMerges.ToArray()))
                     {
                         updateProgressBar(tpb, 0, "Salvando Arquivo.");
                         doc.Save("mergepdf.pdf", FileFormat.PDF);
                         MessageBox.Show("Arquivo salvo com sucesso!");
                         updateProgressBar(tpb, 100, "Arquivo Salvo");
                     }
                 }).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                button1.Enabled = true;
            }
            button1.Invoke(new Action(() =>
            {
                button1.Enabled = true;
            }));

        }
        private void Zip_ExtractProgress(object sender, ExtractProgressEventArgs e)
        {
            // tpb.CustomText = "Extraindo";


            if (e.EventType == ZipProgressEventType.Extracting_EntryBytesWritten)
            {

                if (e.BytesTransferred == e.TotalBytesToTransfer)
                {
                    //tpb.Value = (int)(iZip++ / (0.01 * totalZip));
                    //updateProgressBar(tpb, (int)(iZip++ / (0.01 * totalZip)));
                    qtdZip = (int)(iZip++ / (0.01 * totalZip));
                }
                //tpb.Value = (int)(e.BytesTransferred / (0.01 * e.TotalBytesToTransfer));
            }
            if (e.EventType == ZipProgressEventType.Extracting_BeforeExtractAll)
            {
                updateProgressBar(tpb, qtdZip, "Extraindo arquivo.");
                //tpb.CustomText = "Extraindo arquivo.";
            }
            if (e.EventType == ZipProgressEventType.Extracting_BeforeExtractEntry)
            {
                //tpb.CustomText = $"Extraindo arquivo: {e.CurrentEntry.FileName}";
                //tpb.CustomText = $"Extraindo arquivos {e.CurrentEntry.FileName} - ({iZip}/{totalZip})";
                updateProgressBar(tpb, qtdZip, $"Extraindo arquivos {e.CurrentEntry.FileName} - ({iZip}/{totalZip})");
                totalZip = e.EntriesTotal;
            }

            //tpb.Value = (int)(1.0d / e.TotalBytesToTransfer * e.BytesTransferred * 100.0d);
        }
        private void updateProgressBar(TextProgressBar sender, int? valor = null, string text = null)
        {
            if (sender.InvokeRequired)
                sender.Invoke(new Action(() =>
                {
                    sender.Value = valor.GetValueOrDefault();
                    sender.CustomText = text;
                }));
            else
            {
                sender.Value = valor.GetValueOrDefault();
                sender.CustomText = text;
            }
        }
        private void updateLabel(Label label, string value)
        {
            if (label.InvokeRequired)
                label.Invoke(new Action(() =>
                {
                    if (value.Equals("✅"))
                    {
                        label.ForeColor = Color.Green;
                    }
                    else
                    {
                        label.Text += value;
                    }

                }));
            else
            {
                if (value.Equals("✅"))
                {
                    label.ForeColor = Color.Green;
                }
                else
                {
                    label.Text += value;
                }
            }
        }

        private void Wc_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            tpb.CustomText = "Baixando arquivo";
            tpb.Value = e.ProgressPercentage;
        }
        private async Task<List<string>> GetSiteSTJNum(DateTime date)
        {
            List<string> Ids = new List<string>();
            List<string> acordaos = new List<string>(); // pra att futuras.
            var values = new Dictionary<string, string>
            {
                {"aplicacao","dj.resultados"},
                {"data_pesquisa_texto", date.ToString("dd/MM/yyyy")},
                {"sel_tipo_pesquisa","tipo_documento"},
                {"parametro_tela",null},
                {"parametro","5"},
                {"desc_parametro","EMENTA / ACORDÃO"},
                {"tipo_operacao_fonetica","C"},
                {"nu_pagina_atual","0"},
                {"proximo","TRUE"},
                {"tipo_pesquisa","tipo_documento" },
                {"data_pesquisa", date.ToString("dd/MM/yyyy")},
                {"data_pesquisa_01",date.ToString("dd/MM/yyyy")},
                {"data_pesquisa_fim",null},
                {"padrao_data","padrao_data_publicacao"},
                {"padrao_tela_documentos","padrao_tela_documentos_1_1"}
            };
            using (var client = new HttpClient())
            {
                var response = await client.PostAsync("https://ww2.stj.jus.br/processo/dj/consulta/documento/tipo", new FormUrlEncodedContent(values));

                var getString = await response.Content.ReadAsStringAsync();

                var doc = new HtmlAgilityPack.HtmlDocument();
                if (getString.Contains("Sem ocorr&ecirc;ncias."))
                {
                    return null;
                }
                doc.LoadHtml(getString);

                var nodes = doc.DocumentNode.SelectNodes("//div");
                DiarioDisponivel = doc.DocumentNode.SelectNodes(@"/html/body/div[2]/div[6]/div/div/div[3]/div[2]/div/div/div/form/div[1]/div/span[1]/div[2]/b")[0].InnerText;

                foreach (var nos in nodes)
                {
                    if (nos.Id.Contains("node"))
                    {
                        foreach (var childNos in nos.ChildNodes)
                        {
                            if (childNos.Attributes.Count > 0)
                                if (childNos.Attributes[0].Name.Equals("class") && childNos.Attributes[0].Value.Equals("clsDjArvoreSubCapituloTexto"))
                                {
                                    
                                    if (childNos.InnerText == "Sexta Turma" || childNos.InnerText == "Quinta Turma")
                                    {
                                        label3.Text = $"Informações: Quantidade de Acórdãos {Ids.Count} | Ultima Turma: {acordaos[acordaos.Count - 1]}";
                                        return Ids;
                                    }
                                    acordaos.Add(childNos.InnerText);
                                    //break;
                                }
                        }
                    }

                    if (nos.ChildNodes.Count > 3)
                    {
                        foreach (var childNos in nos.ChildNodes)
                        {
                            if (childNos.ChildNodes.Count > 3)
                            {
                                foreach (var childchildNos in childNos.ChildNodes)
                                {
                                    if (childchildNos.Id.Contains("idDjArvoreDocumentoLinkImagem"))
                                    {

                                        var getId = childchildNos.LastChild.Id;
                                        var trataId = getId.Split('_');
                                        if (trataId.Length == 4)
                                        {
                                            if (Ids.Count == 0)
                                            {

                                                label2.Text = $"Primeira Parte: {childchildNos.InnerText.Replace("&#8209;", "-")}";
                                            }

                                            Ids.Add(trataId[2]);
                                            label1.Text = $"Segunda Parte: {childchildNos.InnerText.Replace("&#8209;", "-")}";
                                        }
                                        else
                                        {
                                            throw new Exception("Erro ao pegar id do arquivo.");
                                        }
                                        ///
                                        //var id = childchildNos.InnerHtml;
                                        //Regex regex = new Regex(@"\('\d{8}',");
                                        //Match match = regex.Match(id);
                                        //if (match.Success)
                                        //{
                                        //}

                                    }
                                    //if (childchildNos.Id.Contains("node"))
                                    //{
                                    //    foreach(var childNosAcordao in childchildNos.ChildNodes)
                                    //    {
                                    //        if(childNosAcordao.Attributes.Count > 0)
                                    //        if(childNosAcordao.Attributes[0].Name.Equals("class") && childNosAcordao.Attributes[0].Value.Equals("clsDjArvoreSubCapituloTexto"))
                                    //        {
                                    //            acordaos.Add(childNosAcordao.InnerText);
                                    //                break;
                                    //        }
                                    //    }
                                    //}
                                }
                            }
                        }
                    }
                }

            }
            label3.Text = $"Informações: Quantidade de Acórdãos {Ids.Count} | Ultima Turma: {acordaos[acordaos.Count -1]}";
            if(Ids.Count > 0)
            {
                return Ids;
            }
            return null;
        }


        private async Task<Tuple<string, string>> GetSiteSTJ(DateTime date)
        {
            Tuple<string, string> result = default;
            var values = new Dictionary<string, string>
            {
                {"aplicacao","dj.resultados"},
                {"data_pesquisa_texto", date.ToString("dd/MM/yyyy")},
                {"sel_tipo_pesquisa","tipo_documento"},
                {"parametro_tela",null},
                {"parametro","5"},
                {"desc_parametro","EMENTA / ACORDÃO"},
                {"tipo_operacao_fonetica","C"},
                {"nu_pagina_atual","0"},
                {"proximo","TRUE"},
                {"tipo_pesquisa","tipo_documento" },
                {"data_pesquisa", date.ToString("dd/MM/yyyy")},
                {"data_pesquisa_01",date.ToString("dd/MM/yyyy")},
                {"data_pesquisa_fim",null},
                {"padrao_data","padrao_data_publicacao"},
                {"padrao_tela_documentos","padrao_tela_documentos_1_1"}
            };
            HttpClient client = new HttpClient();
            var response = await client.PostAsync("https://ww2.stj.jus.br/processo/dj/consulta/documento/tipo", new FormUrlEncodedContent(values));

            var getString = await response.Content.ReadAsStringAsync();

            var doc = new HtmlAgilityPack.HtmlDocument();
            if (getString.Contains("Sem ocorr&ecirc;ncias."))
            {
                return null;
            }
            doc.LoadHtml(getString);
            //var PaginaPrincipalnodes = doc.DocumentNode.SelectNodes("//*[@id=\"idDjPaginadoBlcoPrincipal\"]");
            //var nodes = PaginaPrincipalnodes[0].SelectNodes("//span/div");
            var nodes = doc.DocumentNode.SelectNodes("//div");
            DiarioDisponivel = doc.DocumentNode.SelectNodes(@"/html/body/div[2]/div[6]/div/div/div[3]/div[2]/div/div/div/form/div[1]/div/span[1]/div[2]/b")[0].InnerText;
            string primeiraParte = default;
            string ultimaParte = default;
            bool primeiro = true;
            bool nTeveQuintaTurma = false;
            bool onlyQuartaTurma = false;
            for (int i = 0; i < nodes.Count; i++)
            {
                //if (nodes[i].InnerText.Equals("Coordenadoria da Corte Especial"))
                //{
                //    primeiraParteCDE = nodes[i + 2].InnerText.Replace("  ", "").Replace("\n", "").Trim();
                //    fimCDE = true;
                //}
                //if (nodes[i].InnerText.Equals("Acórdãos") && fimCDE)
                //{
                //    ultimaParteCDE = nodes[i - 1].InnerText.Replace("  ", "").Replace("\n", "").Trim();
                //    fimCDE = false;
                //}
                //if (nodes[i].InnerText.Equals("Primeira Turma"))
                //{
                //    primeiraParte = nodes[i + 2].InnerText.Replace("  ", "").Replace("\n", "").Trim();
                //}
                //if(nodes[i].InnerText.Equals("Segunda Turma") && primeiraParte == null)
                //{
                //    primeiraParte = nodes[i + 2].InnerText.Replace("  ", "").Replace("\n", "").Trim();
                //}
                //if (nodes[i].InnerText.Equals("Quinta Turma"))
                //{
                //    ultimaParte = nodes[i - 3].InnerText.Replace("  ", "").Replace("\n", "").Trim();
                //    break;
                //}

                if (nodes[i].InnerText.Equals("Acórdãos") && primeiro)
                {
                    primeiraParte = nodes[i + 4].InnerText.Replace("  ", "").Replace("\n", "").Trim();
                    primeiro = false;
                }
                if (nodes[i].InnerText.Equals("Primeira Turma"))
                {
                    ultimaParte = nodes[i - 3].InnerText.Replace("  ", "").Replace("\n", "").Trim();
                }
                if (nodes[i].InnerText.Equals("Segunda Turma"))
                {
                    ultimaParte = nodes[i - 3].InnerText.Replace("  ", "").Replace("\n", "").Trim();
                }
                if (nodes[i].InnerText.Equals("Terceira Turma"))
                {
                    ultimaParte = nodes[i - 3].InnerText.Replace("  ", "").Replace("\n", "").Trim();
                }
                if (nodes[i].InnerText.Equals("Quarta Turma"))
                {
                    ultimaParte = nodes[i - 3].InnerText.Replace("  ", "").Replace("\n", "").Trim();
                }
                if (nodes[i].InnerText.Equals("Quinta Turma"))
                {
                    nTeveQuintaTurma = true;
                    ultimaParte = nodes[i - 3].InnerText.Replace("  ", "").Replace("\n", "").Trim();
                }
                if (nodes[i].InnerText.Equals("Sexta Turma") && !nTeveQuintaTurma)
                {
                    nTeveQuintaTurma = true;
                    ultimaParte = nodes[i - 3].InnerText.Replace("  ", "").Replace("\n", "").Trim();
                }
                if (!primeiro && nodes[i].InnerText.Contains("Di&aacute;rio publicado em") && !onlyQuartaTurma && !nTeveQuintaTurma)
                {
                    onlyQuartaTurma = true;
                    ultimaParte = nodes[i - 2].InnerText.Replace("  ", "").Replace("\n", "").Trim();
                }

            }
            if (primeiraParte != default && ultimaParte != default)
            {
                Regex regex = new Regex(@"\(([^)]+)\)");
                Match matchPrimeiraParte, matchUltimaParte;
                // remove codigo para o -
                primeiraParte = primeiraParte.Replace("&#8209;", "-");
                ultimaParte = ultimaParte.Replace("&#8209;", "-");
                matchPrimeiraParte = regex.Match(primeiraParte);
                matchUltimaParte = regex.Match(ultimaParte);

                if (!matchPrimeiraParte.Success)
                {
                    throw new Exception("Não foi possivel extraír a informação da primeira turma.");
                }
                if (!matchUltimaParte.Success)
                {
                    throw new Exception("Não foi possivel extraír a informação da quarta turma.");
                }
                label1.Text = $"Segunda Parte: {ultimaParte}";
                label2.Text = $"Primeira Parte: {primeiraParte}";

                result = new Tuple<string, string>(matchPrimeiraParte.Value.Replace("(", "").Replace(")", "").Replace("&#8209;", "-"), matchUltimaParte.Value.Replace("(", "").Replace(")", "").Replace("&#8209;", "-"));

            }
            else
            {
                throw new Exception("Não foi possível localizar os Acórdãos");
            }

            return result;
        }
    }
}
