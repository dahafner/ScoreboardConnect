using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.Concurrent;
using System.Data;
using ScoreboardLiveApi;
using TP;
using System.Threading.Tasks;
using System.Threading;
using System.Net.Sockets;
using System.Net;
using System.IO.Compression;
using System.IO;
using System.Xml;
using System.Linq;
using TP.Data;

namespace TP
{
    public class TPListener2
    {
        private CancellationTokenSource m_doCancel;
        private Dictionary<string, int> m_activeCourts = new Dictionary<string, int>();

        protected string m_debugFileDestinationDirectory;

        public event EventHandler ServiceStarted;
        public event EventHandler ServiceStopped;
        public event EventHandler<(string, Exception)> ServiceError;
        public event EventHandler<(string, int)> CourtUpdate;
        public event EventHandler<List<EventData>> EventUpdate;

        public bool IsListening
        {
            get
            {
                return (m_doCancel != null) && !m_doCancel.IsCancellationRequested;
            }
        }

        public TPListener2()
        {
        }

        public TPListener2(string debugFilesDestinationFolder) : this()
        {
            m_debugFileDestinationDirectory = debugFilesDestinationFolder;
            if (!m_debugFileDestinationDirectory.EndsWith('\\'))
            {
                m_debugFileDestinationDirectory += '\\';
            }
        }

        public void Start(List<TP.Court> initialCourtSetup = null)
        {
            m_doCancel = new CancellationTokenSource();
            Task.Run(() =>
            {
                ServiceStarted?.Invoke(this, EventArgs.Empty);
                TcpListener tcp = new TcpListener(IPAddress.Any, 13333);
                tcp.Start();
                bool doBreak = false;
                m_activeCourts.Clear();
                initialCourtSetup?.ForEach(initialCourt =>
                {
                    if (initialCourt.TpMatchID > 0)
                    {
                        m_activeCourts.Add(initialCourt.Name, initialCourt.TpMatchID);
                    }
                });
                while (!doBreak)
                {
                    try
                    {
                        tcp.AcceptTcpClientAsync().ContinueWith(r =>
                        {
                            if (r.IsCompleted)
                            {
                                HandleConnection(r.Result);
                            }
                        }, m_doCancel.Token).Wait();
                    }
                    catch (AggregateException e)
                    {
                        e.Handle(innerException =>
                        {
                            if (innerException is TaskCanceledException)
                            {
                                tcp.Stop();
                                doBreak = true;
                            }
                            else
                            {
                                OnServiceError(innerException);
                            }
                            return true;
                        });
                    }
                    catch (Exception e)
                    {
                        OnServiceError(e);
                    }
                }
                ServiceStopped?.Invoke(this, EventArgs.Empty);
            });
        }

        protected void HandleConnection(TcpClient connection)
        {
            var connectionStream = connection.GetStream();
            connectionStream.Read(new byte[4], 0, 4);
            using (var unzip = new GZipStream(connectionStream, CompressionMode.Decompress))
            using (MemoryStream ms = new MemoryStream())
            {
                Stream streamXMLReaderShouldUse = unzip;

                if (Directory.Exists(m_debugFileDestinationDirectory))
                {
                    unzip.CopyTo(ms);
                    string debugFile = string.Format("{1}{0}.xml", DateTime.Now.ToString().Replace(':', '-'), m_debugFileDestinationDirectory);
                    File.WriteAllText(debugFile, Encoding.UTF8.GetString(ms.ToArray()));
                    ms.Position = 0;
                    streamXMLReaderShouldUse = ms;
                }

                XmlReaderSettings xmlSettings = new XmlReaderSettings()
                {
                    // Async = true
                };

                using (XmlReader xmlReader = XmlReader.Create(streamXMLReaderShouldUse, xmlSettings))
                {
                    if (xmlReader.ReadToFollowing("ONCOURT"))
                    {
                        using (XmlReader onCourt = xmlReader.ReadSubtree())
                        {
                            ReadOnCourt(onCourt);
                        }
                    }

                    if (xmlReader.ReadToFollowing("EVENTS"))
                    {
                        using (XmlReader eventsXml = xmlReader.ReadSubtree())
                        {
                            ReadEvents(eventsXml);
                        }
                    }
                }
            }
        }

        protected void ReadOnCourt(XmlReader reader)
        {
            List<string> courtsWithMatchesAssigned = new List<string>();
            while (reader.ReadToFollowing("MATCH"))
            {
                int.TryParse(reader.GetAttribute("ID"), out int tpMatchId);
                if (tpMatchId == 0) continue;

                string tpCourtName = reader.GetAttribute("CT");

                m_activeCourts.TryGetValue(tpCourtName, out int currentMatchId);
                if ((currentMatchId == 0) || (currentMatchId != tpMatchId))
                {
                    m_activeCourts[tpCourtName] = tpMatchId;
                    CourtUpdate?.Invoke(this, (tpCourtName, tpMatchId));
                }
                courtsWithMatchesAssigned.Add(tpCourtName);
            }

            foreach (string nowEmptyCourt in m_activeCourts.Select(kvp => kvp.Key).Except(courtsWithMatchesAssigned))
            {
                m_activeCourts.Remove(nowEmptyCourt);
                CourtUpdate?.Invoke(this, (nowEmptyCourt, 0));
            }
        }

        protected void ReadEvents(XmlReader reader)
        {
            List<EventData> events = new List<EventData>();
            while (reader.ReadToFollowing("EVENT"))
            {
                using (var subtree = reader.ReadSubtree())
                {
                    events.Add(new EventData(subtree));
                }
            }
            EventUpdate?.Invoke(this, events);
        }

        public void Stop()
        {
            if (m_doCancel != null)
            {
                m_doCancel.Cancel();
            }
        }

        protected void OnServiceError(Exception error)
        {
            ServiceError?.Invoke(this, ("An error occured while listening for TP changes", error));
        }

    }
}
