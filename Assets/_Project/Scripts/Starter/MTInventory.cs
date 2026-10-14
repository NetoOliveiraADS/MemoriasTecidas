using UnityEngine;

// Inventário didático. O estado dura apenas durante esta sessão de jogo.
public sealed class MTInventory : MonoBehaviour
{
    public int Cotton { get; private set; }
    public int Yarn { get; private set; }
    public int Kits { get; private set; }
    public int Hammocks { get; private set; }
    public bool KitClaimed { get; private set; }
    public bool HammockMade { get; private set; }
    public bool Delivered { get; private set; }
    private const int Limit = 99;

    public bool TryCollectCotton(out string message)
    {
        if (Cotton >= Limit)
        {
            message = "Você já tem 99 algodões. Prepare fio primeiro.";
            return false;
        }
        Cotton++;
        message = "Algodão recolhido. Cada preparo usa dois algodões.";
        return true;
    }

    public bool TryClaimKit(out string message)
    {
        if (KitClaimed)
        {
            message = "Ana: use dois algodões por fio. Três fios e o kit formam uma rede.";
            return false;
        }
        Kits = 1;
        KitClaimed = true;
        message = "Ana: aqui está seu kit. Recolha seis algodões e prepare três fios.";
        return true;
    }

    public bool TrySpin(out string message)
    {
        // Validar todas as condições antes de alterar os contadores.
        if (Cotton < 2 || Yarn >= Limit)
        {
            message = "Você precisa de dois algodões e espaço para um fio.";
            return false;
        }
        Cotton -= 2;
        Yarn++;
        message = "Fio preparado. Esta é uma simplificação didática do ofício.";
        return true;
    }

    public bool TryWeave(out string message)
    {
        if (HammockMade)
        {
            message = "A rede desta demonstração já foi produzida.";
            return false;
        }
        if (Yarn < 3 || Kits < 1)
        {
            message = "Faltam materiais: a rede exige três fios e um kit.";
            return false;
        }
        Yarn -= 3;
        Kits--;
        Hammocks++;
        HammockMade = true;
        message = "Rede pronta! Leve a peça à Mostra.";
        return true;
    }

    public bool TryDeliver(out string message)
    {
        if (Delivered)
        {
            message = "Sua rede já está na Mostra. Demonstração concluída.";
            return false;
        }
        if (Hammocks < 1)
        {
            message = "Prepare uma rede no tear antes de entregar.";
            return false;
        }
        Hammocks--;
        Delivered = true;
        message = "Você concluiu o percurso da primeira rede!";
        return true;
    }
}
