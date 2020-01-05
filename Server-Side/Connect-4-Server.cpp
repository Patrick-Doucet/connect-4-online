#include <iostream>
#include <string.h>
#include <string>
#include <netinet/in.h>
#include <sys/socket.h>
#include <arpa/inet.h>
#include <thread>
#include <unistd.h>
#include <vector>
#include <mutex>
#include <signal.h>

#define PORT_NUM 10123
#define MAX_LISTEN 23
#define BUFFER_SIZE 1024

struct User
{
    uint32_t     sockfd;
    std::string  ip;
    std::string  username;
};

// GLOBALS
std::vector<User>  g_user_list; // Contient tous les clients connectés et leurs info
std::mutex         mtx_user_list; // Mutex pour éviter la corruption lorsque g_user_list est utilisé par les threads
uint32_t           g_asker = 0; // Moyen pour envoyer de l'info entre les threads. Contient le client_s de celui qui demande à jouer
std::mutex         mtx_asker; // Mutex pour éviter la corruption lorsque g_asker est utilisé par les threads
uint32_t           server_s;

// Remettre un buffer à 0 pour ne pas avoir du texte qui reste d'un message precédent
void clear_buffer(char buf[])
{
    for(auto i = 0; i < BUFFER_SIZE; i++) buf[i] = '\0';
}

// Passe tous les clients connectés, et leur donne un string avec le nombre d'autre clients, puis leur nom et leur IP en utilisant des délimiteurs. 
// Ex: "2!username:IP!username:IP"
// Appellée lorsqu'un client connecte ou déconnecte.
void inform_of_new_client()
{
    std::cout << "Updating client list." << std::endl;
    char out_buf[BUFFER_SIZE];
    mtx_user_list.lock();

    // On passe tous les clients connectés
    for(auto user_to : g_user_list)
    {
        std::string msg = std::to_string(g_user_list.size() - 1) + '!'; // Le nombres de clients connectés, moins lui-même

        for(auto user_about : g_user_list) // On prend l'info des autres
            if(user_to.sockfd != user_about.sockfd) // On ne veut pas envoyer au client sa propre info.
                msg += user_about.username + ':' + user_about.ip + '!'; // On génère le string à envoyer

        msg.pop_back(); // On enlève le '!' de surplus à la fin.
        strcpy(out_buf, msg.c_str());
        send(user_to.sockfd, out_buf, (strlen(out_buf)+1), 0); // On envoie le string au client
        clear_buffer(out_buf);
    }
    mtx_user_list.unlock();
}

// Ajoute le nouvel utilisateur au vecteur de clients, puis informe tous ceux connecté du nouveau client.
void new_user(uint32_t sockfd, std::string ip, std::string username)
{
    std::cout << "User " << username << " is connected." << std::endl;
    mtx_user_list.lock();
    g_user_list.emplace_back(User{sockfd, ip, username});
    mtx_user_list.unlock();
    inform_of_new_client();
}

// Petite fonction pour pouvoir trouver le client_s de l'utilisateur dans notre vecteur de clients
uint32_t find_user_sockfd(std::string username)
{
    mtx_user_list.lock();
    for(auto u : g_user_list)
        if (u.username == username)
        {
            uint32_t socket = u.sockfd;
            mtx_user_list.unlock();
            return socket;
        }
    mtx_user_list.unlock();

    return 0;
}

// Petite fonction pour pouvoir trouver le nom d'utilisateur avec son client_s dans notre vecteur de clients
std::string find_user_name(uint32_t sockfd)
{
    mtx_user_list.lock();
    for (auto u : g_user_list)
        if (u.sockfd == sockfd)
        {   
            std::string user = u.username;
            mtx_user_list.unlock();
            return user;
        }
    mtx_user_list.unlock();

    return "";
}

// Petite fonction pour pouvoir trouver le IP de l'utilisateur avec son client_s dans notre vecteur de clients
std::string find_user_ip(uint32_t sockfd)
{
    mtx_user_list.lock();
    for (auto u : g_user_list)
        if (u.sockfd == sockfd)
        {
            std::string ip = u.ip;
            mtx_user_list.unlock();
            return ip;
        }
    mtx_user_list.unlock();

    return "";
}

// Fonction qui ferme la connection du client, puis le retire du vecteur de clients
void disconnect_client(uint32_t sockfd)
{
    std::string username = find_user_name(sockfd);
    close(sockfd);
    mtx_user_list.lock();
    for(auto user = g_user_list.begin(); user != g_user_list.end(); user++)
    {
        if(user->sockfd == sockfd) 
        {
           g_user_list.erase(user);
           std::cout << "User " << username << " has disconnected." << std::endl;
           break;
        }
    }
    mtx_user_list.unlock();

}

// Fonction qui écoute pour des messages du client.
void listen_for_msg(uint32_t user1_s)
{
    char in_buf[BUFFER_SIZE];
    char out_buf[BUFFER_SIZE];
    while(1)
    {
        // On attend de receoir un message.
        int val = recv(user1_s, in_buf, sizeof(in_buf), 0);
        if(val == -1) // Si le socket ferme de façon innatendue, on enlève le client de notre liste.
        {
            disconnect_client(user1_s);
            return;
        }

        std::string input = std::string(in_buf); // Le message reçi

        // Message du client pour avertir qu'il se déconnecte.
        if (input == "##bye##")
        {
            disconnect_client(user1_s);
            inform_of_new_client();
        }
        
        // La réponse à la demande de jeu
        else if(input == "##accept##" || input == "##reject##")
        {
            mtx_asker.lock();
            uint32_t user2_s = g_asker; // Trouver c'est qui qui avait demandé de jouer
            g_asker = 0;
            mtx_asker.unlock();
            //user2 answers yes or no
            strcpy(out_buf, input.c_str());
            send(user2_s, out_buf, (strlen(out_buf) + 1), 0);

            if (input == "##accept##")
                std::cout << find_user_name(user1_s) << " accepted the game with " << find_user_name(user2_s) << "." << std::endl;
            else
                std::cout << find_user_name(user1_s) << " rejected the game with " << find_user_name(user2_s) << "." << std::endl;

        }

        // Le client demande de jouer avec un autre. Il envoie son nom d'utilisateur.
        else
        {
            // request to user2
            uint32_t    user2_s       = find_user_sockfd(input);
            std::string user1_name    = find_user_name(user1_s) + ':' + find_user_ip(user1_s);
            
            std::cout << find_user_name(user1_s) << " is asking " << find_user_name(user2_s) << " to play." << std::endl;

            // On stock qui demande pour que le thread de celui qui se fait demander save qui c'est.
            // On ne veut pas que l'information se perd si deux joueurs demandent en même temps donc on lock avec un mutex. Une seule demande peut se faire à la fois.
            // La seconde demande se fera lorsque la première est acceptée ou refusée
            mtx_asker.lock();
            while(g_asker != 0)
            {
                mtx_asker.unlock();
                mtx_asker.lock();
            }
            g_asker = user1_s;
            mtx_asker.unlock();
            
            // On envoie le nom d'utilisateur et le IP de celui qui demande au client demandé
            strcpy(out_buf, user1_name.c_str());
            send(user2_s, out_buf, (strlen(out_buf) + 1), 0);
        }

        input.clear();
        clear_buffer(out_buf);
        clear_buffer(in_buf);
    }
}

// Fonction qui est appellé lorsqu'on appuis sur CTRL+C dans l'execusion du serveur
void interrupt_handler(int sig)
{
    char out_buf[BUFFER_SIZE] = {'\0'};
    strcpy(out_buf, "##disconnect##"); // Message à envoyer aux clients pour leur dire que le client se ferme et qu'ils doivent se déconnecter
    std::cout << std::endl << "Server is closing. Disconnecting everyone." << std::endl;
    mtx_user_list.lock();
    for(auto user : g_user_list)
    {
        // Pour chaque client on envoie le message, puis on ferme leur connection
        send(user.sockfd, out_buf, (strlen(out_buf)+1), 0);
        close(user.sockfd);
        std::cout << "Disconnected " << user.username << "." << std::endl;
    }
    mtx_user_list.unlock();

    // On ferme le socket du serveur puis un exit
    close(server_s);
    exit(0); 
}

int main()
{
    signal(SIGINT, &interrupt_handler); // Intercepter CTRL+C

    struct sockaddr_in  server_addr;
    server_s                     = socket(AF_INET, SOCK_STREAM, 0);
    server_addr.sin_family       = AF_INET;
    server_addr.sin_port         = htons(PORT_NUM);
    server_addr.sin_addr.s_addr  = htonl(INADDR_ANY);

    bind(server_s, (struct sockaddr *)&server_addr, sizeof(server_addr));
    listen(server_s, MAX_LISTEN);


    uint32_t             client_s;
    struct sockaddr_in   client_addr;
    struct in_addr	     client_ip_addr;
    uint32_t addr_len    = sizeof(client_addr);

    std::cout << "Server started." << std::endl;

    std::vector<std::thread> client_threads; // Structure pour tiendre compte des threads qu'on créer.

    while(1)
    {
        client_s = accept(server_s, (struct sockaddr *) &client_addr, &addr_len);
        memcpy(&client_ip_addr, &client_addr.sin_addr.s_addr, 4); // IP du client

        //Receive username from client
        char in_buf[BUFFER_SIZE] = {'\0'};
        int val = recv(client_s, in_buf, sizeof(in_buf), 0); // On reçoie le nom d'utilisateur
        if(val == -1) continue;

        // On créer un thread afin d'ajouter un client pour revenir écouter le plus vite possible. Le thread se termine vite.
        client_threads.emplace_back(&new_user, client_s, inet_ntoa(client_ip_addr), std::string(in_buf));

        // Pour chaque client on a un thread qui écoute pour les messages qu'il peut envoyer.
        client_threads.emplace_back(&listen_for_msg, client_s);
    }

    // Précaution en cas où le while(1) se termine.
    for(auto &t : client_threads)
        t.join();

    for(auto user : g_user_list)
        close(user.sockfd);
    close(server_s);

    return 0;
}
