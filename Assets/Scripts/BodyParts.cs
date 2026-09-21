using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;

public class BodyParts : MonoBehaviour
{
    [SerializeField] private Image[] parts;
    [SerializeField] private Color desactiveColor;
    [SerializeField] private ScrapManager scrapManager;

    private bool[] takedPart = new bool[3];
    private bool winEnabled = true;

    public event Action<int, int> PartCollected;

    public int CollectedParts
    {
        get
        {
            int count = 0;
            for (int i = 0; i < takedPart.Length; i++)
            {
                if (takedPart[i]) count++;
            }
            return count;
        }
    }

    public int TotalParts => takedPart.Length;
    public bool HasAllParts => CheckWin();

    public void SetWinEnabled(bool enabled)
    {
        winEnabled = enabled;

        if (winEnabled && CheckWin() && scrapManager != null)
        {
            scrapManager.EndGame(true);
        }
    }

    public void ActualizeParts(int indexOfTakenPart)
    {
        if (indexOfTakenPart < 0 || indexOfTakenPart >= takedPart.Length)
        {
            Debug.LogWarning($"Nieprawidłowy indeks części ciała: {indexOfTakenPart}", this);
            return;
        }

        if (takedPart[indexOfTakenPart])
        {
            return;
        }

        takedPart[indexOfTakenPart] = true;

        int uiCount = parts != null ? Mathf.Min(parts.Length, takedPart.Length) : 0;
        for (int i = 0; i < uiCount; i++)
        {
            if (parts[i] == null)
            {
                continue;
            }

            if (takedPart[i])
            {
                parts[i].color = Color.white;
            }
            else
            {
                parts[i].color = desactiveColor;
            }
        }

        PartCollected?.Invoke(CollectedParts, TotalParts);

        if (winEnabled && CheckWin())
        {
            if (scrapManager != null)
            {
                scrapManager.EndGame(true);
            }
        }
    }

    private bool CheckWin()
    {
        for (int i = 0; i < takedPart.Length; i++)
        {
            if (!takedPart[i])
            {
                return false;
            }
        }

        return takedPart.Length > 0;
    }
}
