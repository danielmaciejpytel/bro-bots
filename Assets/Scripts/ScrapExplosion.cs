using System.Collections.Generic;
using UnityEngine;

public class ScrapExplosion : MonoBehaviour
{
    private static readonly Dictionary<GameObject, GameObject[]> ScrapPieceCache =
        new Dictionary<GameObject, GameObject[]>();

    public GameObject[] scrapMetal;
    public int maxPieces = 100;
    public int minPieces = 75;
    public float yOffset = 8f;
    public bool shouldDropScrap;
    public GameObject scrapToDrop;
    public float itemDropPercent = 50f; // Dodane pole procentowe dla dropu przedmiotu
    public GameObject[] itemsToDrop; // Dodana tablica z przedmiotami do dropu

    private bool scrapDropped = false; // Flaga sprawdzająca, czy scrap został zdropowany

    public void DropScrap(bool force = false)
    {
        if (scrapDropped && !force)
        {
            return;
        }

        if (scrapMetal == null || scrapMetal.Length == 0)
        {
            Debug.LogWarning($"Brak prefabów scrapu w ScrapExplosion na '{name}'.", this);
            scrapDropped = true;
            return;
        }

        int minimum = Mathf.Max(0, Mathf.Min(minPieces, maxPieces));
        int maximum = Mathf.Max(minimum, Mathf.Max(minPieces, maxPieces));
        int piecesToDrop = minimum == maximum
            ? minimum
            : Random.Range(minimum, maximum + 1);

        for (int i = 0; i < piecesToDrop; i++)
        {
            int randomScrap = Random.Range(0, scrapMetal.Length);
            GameObject scrapTemplate = GetScrapPieceTemplate(scrapMetal[randomScrap]);
            if (scrapTemplate == null)
            {
                continue;
            }

            Vector2 spread = Random.insideUnitCircle * 0.35f;
            Vector3 spawnPosition = transform.position +
                                    Vector3.up * yOffset +
                                    new Vector3(spread.x, 0f, spread.y);

            Instantiate(scrapTemplate, spawnPosition, Random.rotation);
        }

        if (shouldDropScrap && itemsToDrop != null && itemsToDrop.Length > 0)
        {
            float dropChance = Random.Range(0f, 100f);
            if (dropChance < itemDropPercent)
            {
                int randomItem = Random.Range(0, itemsToDrop.Length);
                Instantiate(itemsToDrop[randomItem], transform.position, transform.rotation);
            }
        }

        scrapDropped = true; // Ustaw flagę na true po zdropowaniu scrapu
    }

    private static GameObject GetScrapPieceTemplate(GameObject scrapPrefab)
    {
        if (scrapPrefab == null)
        {
            return null;
        }

        if (!ScrapPieceCache.TryGetValue(scrapPrefab, out GameObject[] pieces))
        {
            Scrap[] scrapComponents = scrapPrefab.GetComponentsInChildren<Scrap>(true);
            pieces = new GameObject[scrapComponents.Length];
            for (int i = 0; i < scrapComponents.Length; i++)
            {
                pieces[i] = scrapComponents[i].gameObject;
            }

            ScrapPieceCache[scrapPrefab] = pieces;
        }

        if (pieces.Length == 0)
        {
            return scrapPrefab;
        }

        return pieces[Random.Range(0, pieces.Length)];
    }
}
