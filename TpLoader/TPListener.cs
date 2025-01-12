using System;
using System.Collections.Generic;
using System.Data;
using ScoreboardLiveApi;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.IO.Compression;
using System.IO;
using System.Text;
using System.Xml;

namespace TP
{
    public class TPListener
    {
        public class TPCourtUpdateEventArgs
        {
            public string CourtName { get; set; }
            public Match Match { get; set; }
            public Event Event { get; set; }
            public Draw Draw { get; set; }
        }

        private CancellationTokenSource m_doCancel;
        private string m_debugFileDestinationDirectory;
        private List<Data.CourtData> m_activeCourts = new List<Data.CourtData>();
        private string m_tpFile;
        private Tournament m_initialStateTournament;

        public event EventHandler ServiceStarted;
        public event EventHandler ServiceStopped;
        public event EventHandler<(string, Exception)> ServiceError;
        public event EventHandler<TPCourtUpdateEventArgs> CourtUpdate;
        public event EventHandler<Tournament> TournamentUpdate;


        public bool IsListening
        {
            get
            {
                return (m_doCancel != null) && !m_doCancel.IsCancellationRequested;
            }
        }
        public TPListener(string sourceTPFile, string debugDirectory = null)
        {
            m_debugFileDestinationDirectory = debugDirectory;
            if ((m_debugFileDestinationDirectory != null) && !m_debugFileDestinationDirectory.EndsWith("\\"))
            {
                m_debugFileDestinationDirectory += "\\";
            }
            m_tpFile = sourceTPFile;
        }
        public void Start()
        {
            if (m_doCancel != null)
            {
                throw new Exception("Listener already running");
            }
            m_doCancel = new CancellationTokenSource();
            _ = SetInitialState();
            Run();
        }

        public void Stop()
        {
            if (m_doCancel != null)
            {
                m_doCancel.Cancel();
            }
        }

        private void Run()
        {
            Task.Run(() =>
            {
                ServiceStarted?.Invoke(this, EventArgs.Empty);
                TcpListener tcp = new TcpListener(IPAddress.Any, 13333);
                tcp.Start();
                bool runSignal = true;
                while (runSignal)
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
                                OnServiceStop();
                                runSignal = false;
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
            });
        }

        private void HandleConnection(TcpClient connection)
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
                    // Async = true,
                    IgnoreWhitespace = true
                };

                List<Data.CourtData> courtsToUpdate = null;
                Tournament tournament = null;
                using (XmlReader xmlReader = XmlReader.Create(streamXMLReaderShouldUse, xmlSettings))
                {
                    if (xmlReader.ReadToFollowing("ONCOURT"))
                    {
                        using (XmlReader onCourt = xmlReader.ReadSubtree())
                        {
                            courtsToUpdate = ReadOnCourt(onCourt);
                        }
                    }
                    if (xmlReader.ReadToFollowing("EVENTS"))
                    {
                        tournament = Tournament.LoadFromXML(xmlReader.ReadSubtree());
                    }
                }

                // Send update event
                courtsToUpdate.ForEach(item => OnCourtUpdate(item.Name, tournament.FindMatchByID(item.TpMatchID)));
            }
        }
        private List<Data.CourtData> ReadOnCourt(XmlReader reader)
        {
            // Read from XML
            List<string> courtsWithMatchesAssigned = new List<string>();
            List<Data.CourtData> onCourts = new List<Data.CourtData>();
            while (reader.ReadToFollowing("MATCH"))
            {
                onCourts.Add(new Data.CourtData(reader));
            }

            // Find courts with new matches
            List<Data.CourtData> sendUpdates = new List<Data.CourtData>();
            foreach (Data.CourtData court in onCourts)
            {
                if (court.TpMatchID == 0) continue;
                lock (m_activeCourts)
                {
                    Data.CourtData activeCourt = m_activeCourts.FirstOrDefault(activeCourt => activeCourt.Name == court.Name);
                    if (activeCourt == null)
                    {
                        // Court was previously unassigned, but now has a match
                        m_activeCourts.Add(court);
                        sendUpdates.Add(court);
                    }
                    else if (activeCourt?.TpMatchID != court.TpMatchID)
                    {
                        // Court has got a new match
                        activeCourt.TpMatchID = court.TpMatchID;
                        sendUpdates.Add(court);
                    }
                }
                courtsWithMatchesAssigned.Add(court.Name);
            }

            // Find previously assigned but now empty courts
            lock (m_activeCourts)
            {
                sendUpdates.AddRange(
                  m_activeCourts.FindAll(activeCourt => !courtsWithMatchesAssigned.Contains(activeCourt.Name)).Select(activeCourt =>
                  {
                      activeCourt.TpMatchID = 0;
                      return activeCourt;
                  })
                );
                m_activeCourts.RemoveAll(activeCourt => !courtsWithMatchesAssigned.Contains(activeCourt.Name));
            }

            return sendUpdates;
        }

        private async Task SetInitialState()
        {
            lock (m_activeCourts)
            {
                m_activeCourts.Clear();
            }
            TPFile tpFile = null;
            try
            {
                tpFile = new TPFile(m_tpFile);
                var courts = await tpFile.LoadCourts();

                m_initialStateTournament = await Tournament.LoadFromTP(tpFile);

                lock (m_activeCourts)
                {
                    foreach (Data.CourtData court in courts)
                    {
                        if (court.TpMatchID > 0)
                        {
                            m_activeCourts.Add(court);
                        }
                        Match match = m_initialStateTournament.FindMatchByID(court.TpMatchID);
                        OnCourtUpdate(court.Name, match);
                    }
                }


            }
            catch (Exception e)
            {
                OnServiceError(e, "Could not load initial state for courts from TP file.");
            }
            finally
            {
                if (tpFile != null)
                {
                    tpFile.Close();
                }
            }
        }

        private void OnServiceError(Exception error, string message = "An error occured while listening for TP changes")
        {
            ServiceError?.Invoke(this, (message, error));
        }

        private void OnServiceStop()
        {
            m_doCancel = null;
            ServiceStopped?.Invoke(this, EventArgs.Empty);
        }

        private void OnCourtUpdate(string tpCourtName, Match tpMatch)
        {
            // Find the event and draw for this match using the original data from
            // the TP-file, since the XML-data does not contain all event and draw data needed.
            Event tpEvent = null;
            Draw tpDraw = null;
            if (tpMatch != null)
            {
                tpEvent = m_initialStateTournament.FindEventByID(tpMatch.EventID);
                tpDraw = m_initialStateTournament.FindDrawByID(tpMatch.DrawID);
            }

            CourtUpdate?.Invoke(this, new TPCourtUpdateEventArgs()
            {
                CourtName = tpCourtName,
                Match = tpMatch,
                Event = tpEvent,
                Draw = tpDraw
            });
        }

        private void OnTournamentUpdate(Tournament tournament)
        {
            TournamentUpdate?.Invoke(this, tournament);
        }
    }
}
