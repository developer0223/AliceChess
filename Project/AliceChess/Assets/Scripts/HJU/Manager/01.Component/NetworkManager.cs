﻿using UnityEngine;
using System.Collections;

using Photon;
using Utility;
using Manager;
using Manager.LoginScene;

namespace Network
{
    public class NetworkManager : PunBehaviour
    {
        public string PlayerName { get; set; }

        private static readonly string VERSION = "v1.0";

        private TypedLobby defaultLobby;
        private PhotonInstantiateManager photonInstantiateManager;

        private void Start()
        {
            DontDestroyOnLoad(GetComponent<Transform>().gameObject);

            PhotonNetwork.logLevel = PhotonLogLevel.ErrorsOnly;
            PhotonNetwork.autoJoinLobby = false;
            PhotonNetwork.automaticallySyncScene = true;

            photonInstantiateManager = GetComponent<PhotonInstantiateManager>();

            PhotonNetwork.sendRate = 50;
            PhotonNetwork.sendRateOnSerialize = 50;
            PhotonNetwork.EnableLobbyStatistics = true;
            PhotonNetwork.ConnectUsingSettings(VERSION);

            StartCoroutine(Test());
        }

        private IEnumerator Test()
        {
            while (true)
            {
                Debug.Log(PhotonNetwork.connectionStateDetailed.ToString());
                yield return new WaitForSeconds(0.5f);
            }
        }

        public override void OnConnectedToMaster()
        {
            defaultLobby = new TypedLobby("LobbyName", LobbyType.Default);
        }

        public override void OnJoinedLobby()
        {
            // 룸 정보 보여주기
        }

        public override void OnJoinedRoom()
        {
            GameObject camera = Instantiate((GameObject)Resources.Load("Camera"));
            Transform cameraTransform = camera.GetComponent<Transform>();
            cameraTransform.position = DataManager.GameCamera.position;

            SendRPC("InstantiateCommonObjects", PhotonTargets.All);

            if (PhotonNetwork.isMasterClient)
            {
                Debug.Log("Client : MasterClient");
                DataManager.MyTeam = DataManager.Team.White;

                photonInstantiateManager.InstantiateWhite();
                cameraTransform.rotation = DataManager.GameCamera.whiteRotation;
            }
            else
            {
                Debug.Log("Client : GuestClient");
                DataManager.MyTeam = DataManager.Team.Black;

                photonInstantiateManager.InstantiateBlack();
                cameraTransform.rotation = DataManager.GameCamera.blackRotation;

                // send rpc startGame
                SendRPC("StartGame", PhotonTargets.All);
            }
        }

        public void JoinLobby()
        {
            PlayerName = Utility.DataManager.playerName;
            PhotonNetwork.playerName = PlayerName;
            PhotonNetwork.JoinLobby(defaultLobby);
        }

        public void CreateRoom(string roomName)
        {
            RoomOptions roomOptions = new RoomOptions()
            {
                IsVisible = true,
                MaxPlayers = 2
            };
            PhotonNetwork.CreateRoom(roomName, roomOptions, defaultLobby);
        }

        public void JoinRoom(string roomName)
        {
            PhotonNetwork.JoinRoom(roomName);
        }

        public void GetRoomList()
        {
            RoomInfo[] infos = PhotonNetwork.GetRoomList();
            foreach (RoomInfo info in infos)
            {
                Debug.Log($"info.Name : {info.Name}");
                Debug.Log($"info.PlayerCount : {info.PlayerCount}");
                Debug.Log($"info.MaxPlayers : {info.MaxPlayers}");
                Debug.Log($"info.IsVisible : {info.IsVisible}");
                Debug.Log($"info.IsOpen : {info.IsOpen}");
            }
        }

        /// <summary>
        /// Internal to send an RPC on given PhotonView.
        /// </summary>
        /// <param name="view">photonView</param>
        /// <param name="methodName">string methodName</param>
        /// <param name="target">PhotonTargets.?</param>
        /// <param name="encrypt">encrypt</param>
        /// <param name="parameters">params</param>
        public void SendRPC(string methodName, PhotonTargets target, params object[] parameters)
        {
            if (!photonView)
            {
                Debug.Log($"Can't send RPC to {target.ToString()} players because photonView is null");
                return;
            }
            photonView.RPC(methodName, target, parameters);
        }

        [PunRPC]
        public IEnumerator StartGame()
        {
            yield return new WaitForSeconds(Utility.DataManager.Time.delayTime);
            Debug.Log("Game Start!");
        }

        [PunRPC]
        public void InstantiateCommonObjects()
        {
            photonInstantiateManager.InstantiateBoard(false);
        }
    }
}