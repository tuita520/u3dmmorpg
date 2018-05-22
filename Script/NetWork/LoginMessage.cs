#region using

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using ClientService;
using ScorpionNetLib;
using Shared;
using UnityEngine;

#endregion

public partial class NetManager : ClientAgentBase, ILogin9xServiceInterface, ILogic9xServiceInterface,
                                  IScene9xServiceInterface, IRank9xServiceInterface, IActivity9xServiceInterface,
                                  IChat9xServiceInterface, ITeam9xServiceInterface
{
    public string ChooseGateAddress = "";
    private bool testGateConnected = false;
    private DateTime testGateOutTime;
    private bool isGateChosing = false;

    public IEnumerator PrepareEnterScene(ulong clientId)
    {
        Logger.Debug("---------------PrepareEnterScene.");
        return null;
        //throw new NotImplementedException();
    }


    public void TestConnectGate(string addr)
    {
        SocketClient client = null;
        try
        {
            var splittedAddress = addr.Trim().Split(':');
            var ip = splittedAddress[0].Trim();
            var port = Convert.ToInt32(splittedAddress[1].Trim());
            var settings = new SocketClientSettings(new IPEndPoint(Dns.GetHostAddresses(ip)[0], port));
            client = new SocketClient(settings);
        }
        catch (Exception exception)
        {
            Logger.Error(exception.ToString());
            return;
        }

        client.OnException += ex =>
        {
            Logger.Error(ex.ToString());
        };

        client.OnConnected += () =>
        {
            if (testGateConnected == false)
            {
                testGateConnected = true;

                ChooseGateAddress = addr;
            }
        };

        try
        {
            client.StartConnect();
        }
        catch (Exception ex)
        {
            Logger.Error(ex.ToString());
        }
    }

    public Coroutine ChooseGateAddr(string gateAddress, TimeSpan span)
    {
        if (isGateChosing)
            return null;

        const int onceTest = 3;
        isGateChosing = true;
        ChooseGateAddress = "";
        testGateConnected = false;

        var co = StartCoroutine(ConnectGateAddr(gateAddress, onceTest, span));
        return co;
    }

    private IEnumerator ConnectGateAddr(string gateAddress, int onceTest, TimeSpan span)
    {
        var address = gateAddress.Trim().Split(';');
        var addressArray = address.Where(s => !string.IsNullOrEmpty(s)).ToList();
        var tempAddressArray = new List<string>(addressArray);
        while (tempAddressArray.Count > 0)
        {
            var tempAddress = new List<string>(onceTest);
            for (var i = 0; i < onceTest && tempAddressArray.Count > 0; ++i)
            {
                var idx = MyRandom.Random(0, 10000) % tempAddressArray.Count;
                tempAddress.Add(tempAddressArray[idx]);
                tempAddressArray.RemoveAt(idx);
            }

            foreach (var addr in tempAddress)
            {
                TestConnectGate(addr);
            }

            testGateOutTime = DateTime.Now + span;
            yield return StartCoroutine(Wait2());
            if (testGateConnected)
            {
                isGateChosing = false;
                yield break;
            }
        }

        if (address.Length > 0)
        {
            ChooseGateAddress = addressArray[MyRandom.Random(0, addressArray.Count - 1)];
        }

        isGateChosing = false;
        yield break;
    }

    private IEnumerator Wait2()
    {
        while (!testGateConnected && testGateOutTime > DateTime.Now)
        {
            yield return null;
        }
    }
}