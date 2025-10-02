using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class SceneTransitionManager : NetworkBehaviour
{
    public static SceneTransitionManager Instance;

    private bool isTransitioning = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void LoadGameSceneServerRpc(ServerRpcParams rpcParams = default)
    {
        if (isTransitioning) return;

        isTransitioning = true;
        PrepareForSceneTransitionClientRpc();

        StartCoroutine(LoadGameSceneCoroutine());
    }

    [ClientRpc]
    private void PrepareForSceneTransitionClientRpc()
    {
        CleanupNetworkObjects();
    }

    private IEnumerator LoadGameSceneCoroutine()
    {
        yield return new WaitForSeconds(0.5f);

        NetworkManager.SceneManager.LoadScene("GameScene", UnityEngine.SceneManagement.LoadSceneMode.Single);
        StartCoroutine(ResetTransitionFlag());
    }

    private IEnumerator ResetTransitionFlag()
    {
        yield return new WaitForSeconds(2f);
        isTransitioning = false;
    }

    private void CleanupNetworkObjects()
    {
        var networkObjects = FindObjectsOfType<NetworkObject>();
        foreach (var netObj in networkObjects)
        {
            if (netObj.IsSpawned && netObj.gameObject.scene.name != "DontDestroyOnLoad")
            {
                if (NetworkManager.Singleton.IsServer)
                {
                    netObj.Despawn(true);
                }
            }
        }
    }
}