sudo -i // root oturumunu açar

sudo apt update

sudo apt install curl wget apt-transport-https

sudo apt install openjdk-17-jre-headless -y

java -version

curl -fsSL https://artifacts.elastic.co/GPG-KEY-elasticsearch | sudo apt-key add -

echo "deb https://artifacts.elastic.co/packages/7.x/apt stable main" | sudo tee -a /etc/apt/sources.list.d/elastic-7.x.list

apt install elasticsearch -y

nano /etc/elasticsearch/elasticsearch.yml //dosyası açılır aşağıdaki değerler eklenir.

cluster.name: graylog

action.auto_create_index: false

xpack.security.enabled: false

xpack.security.transport.ssl.enabled: false

xpack.security.http.ssl.enabled: false

//ctrl+x ile exit yapılır ve "y" ile save enter kaydeder.

systemctl daemon-reload

systemctl enable elasticsearch

systemctl start elasticsearch

systemctl status elasticsearch

curl -X GET http://localhost:9200 //elasticsearch çalışıyorsa kontrol edilir.

//mongodb

wget -qO - https://www.mongodb.org/static/pgp/server-4.4.asc | sudo apt-key add -

echo "deb [ arch=amd64,arm64 ] https://repo.mongodb.org/apt/ubuntu focal/mongodb-org/4.4 multiverse" | sudo tee /etc/apt/sources.list.d/mongodb-org-4.4.list

echo "deb http://security.ubuntu.com/ubuntu focal-security main" | sudo tee /etc/apt/sources.list.d/focal-security.list

curl -fsSL https://pgp.mongodb.com/server-6.0.asc | \

sudo gpg --dearmor -o /etc/apt/trusted.gpg.d/mongodb-server-6.0.gpg

echo "deb [ arch=amd64,arm64 signed=/etc/apt/trusted.gpg.d/keyrings/mongodb-server-6.0.gpg ] https://repo.mongodb.org/apt/ubuntu jammy/mongodb-org/6.0 multiverse" | sudo tee /etc/apt/sources.list.d/mongodb-org-6.0.list

sudo apt update -y

sudo apt upgrade -y

sudo apt-get install gnupg libssl1.1 -y

sudo apt install mongodb-org -y

sudo systemctl start mongod

sudo systemctl status mongod

sudo systemctl enable mongod

wget https://packages.graylog2.org/repo/packages/graylog-5.2-repository_latest.deb

sudo dpkg -i graylog-5.2-repository_latest.deb

sudo apt-get update

sudo apt install graylog-server -y

apt install pwgen -y

< /dev/urandom tr -dc A-Z-a-z-0-9 | head -c${1:-96};echo;

pwgen -N 1 -s 96

password_secret = AOzLxMSwebAJ8RoRPA5n9m97YTLnatVmCpETyPaI5lJPf9RUXOCODCweAT7HCzEEGBvqD15ZWFoxanTAipmVB8JliDvmdvGv

echo -n GryLg24** | sha256sum | cut -d" " -f1

root_password_sha2 = 44ec294a3e2ed866b66bd0e5e477525b4ab0849de9d7808888dd92236b706de8

nano /etc/graylog/server/server.conf

elasticsearch_hosts = http://localhost:9200

systemctl daemon-reload

systemctl start graylog-server

systemctl status graylog-server

systemctl enable graylog-server

apt install nginx -y

nano /etc/nginx/sites-available/graylog.conf

server { listen 80; server_name graylogaudit.cevre.gov.tr;

location / { proxy_set_header Host $http_host; proxy_set_header X-Forwarded-Host $host; proxy_set_header X-Forwarded-Server $host; proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for; proxy_set_header X-Graylog-Server-URL http://$server_name/; proxy_pass http://127.0.0.1:9000; }

}

ln -s /etc/nginx/sites-available/graylog.conf /etc/nginx/sites-enabled/

rm -rf /etc/nginx/sites-enabled/default

systemctl restart nginx

systemctl status nginx
