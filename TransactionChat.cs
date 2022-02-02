using PusherServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using web.Models;
using web.Models.CloudPayment;
using web.Models.Pusher;
using web.Processing; 

namespace web.Logic
{
    public class TransactionChat
    {
        private Pusher pusher;
        private BookingMessageDB bdb;
        public TransactionChat()
        { 
            var options = new PusherOptions
            {
                Cluster = "ap2" 
            };
            pusher = new Pusher(
              "1040580",
              "0a0d35afd76319bf0b45",
              "8640a8f0befcab114411",
              options
            );
            bdb = new BookingMessageDB();
        }
        private int InsertStatus(MessageTypes message, int? refBooking, int from, int to, int refRealty, ChatElement cm,string socket_id)//все сообщения отправляются этим методом
        {
            pusher.TriggerAsync(
                          String.Format("private-chat-with{0}", to),
                          "new_message",
                           cm,
                          new TriggerOptions() { SocketId = socket_id });
            FireBaseDB.FireBaseDeviceModel fbdm = (new FireBaseDB()).getDeviceID(to, refRealty,from);
            Notification fn = new Notification()
            {
                 deviceId = fbdm.deviceId,
                 iswoner= fbdm.isowner,
                 refRealty = refRealty,
                contact = from,
                body = cm.body ,
                title="Новое сообщение от "+ fbdm.senderName
            };
            (new FireBase()).SendNotification(fn);
            return bdb.insertStatus(message, refBooking, from, to, refRealty);
        }

        private String getConvoChannel(int user_id)
        { 
            return String.Format("private-chat-with{0}", user_id); 
        }
        /// <summary>
        /// Запрос на контракт
        /// </summary>
        internal Models.RequestResult Book(bookModel2 bm, int senderid)
        {
            int idBook = bdb.addBooking(bm, senderid);
            if (idBook != 0)
            {
                if (bm.isGuest)
                    InsertStatus(MessageTypes.requestGuest, idBook, senderid, bm.to, bm.refRealty,new ChatElement()
                    {
                        dateFrom=bm.dateFrom.ToString("yyyy-MM-dd'T'HH:mm:ss"),
                        dateTo=bm.dateTo.ToString("yyyy-MM-dd'T'HH:mm:ss"),
                        idbook=idBook,
                        refReciever=bm.to,
                        refSender = senderid,
                        messageType = MessageTypes.requestGuest,
                        mine=false,
                        price=bm.price,
                        tm=DateTime.Now.ToString("yyyy-MM-dd'T'HH:mm:ss"),
                        image=""

                    },bm.socket_id);
                if (!bm.isGuest)
                    InsertStatus(MessageTypes.requestOwner, idBook, senderid, bm.to, bm.refRealty, new ChatElement()
                    {
                        dateFrom = bm.dateFrom.ToString("yyyy-MM-dd'T'HH:mm:ss"),
                        dateTo = bm.dateTo.ToString("yyyy-MM-dd'T'HH:mm:ss"),
                        idbook = idBook,
                        refReciever = bm.to,
                        refSender = senderid,
                        messageType = MessageTypes.requestGuest,
                        mine = false,
                        price = bm.price,
                        tm = DateTime.Now.ToString("yyyy-MM-dd'T'HH:mm:ss"),
                        image = ""

                    }, bm.socket_id);
                return new Models.RequestResult()
                {
                    ResultCode = 1
                };
            }
            else
            {
                return new Models.RequestResult()
                {
                    ResultCode = 0,
                    ResultMessage = "16102020052:ошибка записи сообщения в базу"
                };
            }
        }

        internal void PayNotification(int transactionId,string socket_id,int refUser)
        {
            RequestResult< BookingApplication> ba = (new BookingDb()).getBookInfoByTransaction(transactionId, refUser);
            if (ba.ResultCode == 1)
            {
                InsertStatus(MessageTypes.payment, ba.Result.appid, 100007, ba.Result.refRealtyGuest, ba.Result.refRealty, new ChatElement()
                {
                    idbook = ba.Result. appid,
                    refReciever = ba.Result.refRealtyGuest,
                    refSender = 100007,
                    messageType = MessageTypes.payment,
                    mine = false,
                    tm = DateTime.Now.ToString("yyyy-MM-dd'T'HH:mm:ss")

                }, socket_id);
                InsertStatus(MessageTypes.payment, ba.Result.appid, 100007, ba.Result.refRealtyOwner, ba.Result.refRealty, new ChatElement()
                {
                    idbook = ba.Result.appid,
                    refReciever = ba.Result.refRealtyOwner,
                    refSender = 100007,
                    messageType = MessageTypes.payment,
                    mine = false,
                    tm = DateTime.Now.ToString("yyyy-MM-dd'T'HH:mm:ss")
                }, socket_id);
            }
            
        }

        internal void PayNotification(ChargeModel cm, int refUser)
        {
            RequestResult<BookingApplication> ba = (new BookingDb()).getBookInfo(cm.refBooking, refUser);
            InsertStatus(MessageTypes.payment, cm.refBooking, 100007, ba.Result. refRealtyGuest, ba.Result.refRealty, new ChatElement()
            {
                idbook = cm.refBooking,
                refReciever = ba.Result.refRealtyGuest,
                refSender = 100007,
                messageType = MessageTypes.payment,
                mine = false,
                tm = DateTime.Now.ToString("yyyy-MM-dd'T'HH:mm:ss")

            }, cm.socket_id);
            InsertStatus(MessageTypes.payment, cm.refBooking, 100007, ba.Result.refRealtyOwner, ba.Result.refRealty, new ChatElement()
            {
                idbook = cm.refBooking,
                refReciever = ba.Result.refRealtyOwner,
                refSender = 100007,
                messageType = MessageTypes.payment,
                mine = false,
                tm = DateTime.Now.ToString("yyyy-MM-dd'T'HH:mm:ss") 
            }, cm.socket_id);
        }

        internal Models.RequestResult Pay(PaymentModel payment, int senderid)
        { 
            InsertStatus(MessageTypes.paymentAccept, payment.bookId, senderid, payment.to,  payment.refRealty, new ChatElement()
            {
                idbook = payment.bookId,
                refReciever = payment.to,
                refSender = senderid,
                messageType = MessageTypes.paymentAccept,
                mine = false,
                tm = DateTime.Now.ToString("yyyy-MM-dd'T'HH:mm:ss")

            }, payment.socket_id);
            InsertStatus(MessageTypes.rateRealty, payment.bookId, senderid, payment.to,  payment.refRealty, new ChatElement()
            {
                idbook = payment.bookId,
                refReciever = payment.to,
                refSender = senderid,
                messageType = MessageTypes.rateRealty,
                mine = false,
                tm = DateTime.Now.ToString("yyyy-MM-dd'T'HH:mm:ss")

            }, payment.socket_id);
            return new Models.RequestResult()
            {
                ResultCode = 1
            }; 
        }

        internal Models.RequestResult Rate(BookRate rate, int senderid)
        {
            int idStatus = 0;
            idStatus =  InsertStatus(MessageTypes.rateRealty, rate.bookId, senderid, rate.to, rate.refRealty, new ChatElement()
            { 
                idbook = rate.bookId,
                refReciever = rate.to,
                refSender = senderid,
                messageType = MessageTypes.ratefinished,
                mine = false,
                tm = DateTime.Now.ToString("yyyy-MM-dd'T'HH:mm:ss") 

            }, rate.socket_id);
            if (idStatus != 0)
                return bdb.Rate(rate, idStatus);
            else
                return new Models.RequestResult()
                {
                    ResultCode = 0,
                    ResultMessage = "16102020148:ошибка записи сообщения в базу"
                };
        }

        internal Models.RequestResult RateUser(UserRate rate, int senderid)
        {
            int idStatus = 0;
            idStatus = InsertStatus(MessageTypes.rateUser, 0, senderid, rate.to, 0, new ChatElement()
            {
                idbook = 0,
                refReciever = rate.to,
                refSender = senderid,
                messageType = MessageTypes.rateUser,
                mine = false,
                tm = DateTime.Now.ToString("yyyy-MM-dd'T'HH:mm:ss")

            }, rate.socket_id);
            if (idStatus != 0)
                return bdb.RateUser(rate, idStatus);
            else
                return new Models.RequestResult()
                {
                    ResultCode = 0,
                    ResultMessage = "16102020148:ошибка записи сообщения в базу"
                };
        }
        internal Models.RequestResult SendMessage(messageModel2 msg, int senderId)
        {
            int idStatus = InsertStatus(MessageTypes.message, null, senderId, msg.to, msg.refRealty, new ChatElement()
            { 
                refReciever = msg.to,
                refSender = senderId,
                messageType = MessageTypes.ratefinished,
                mine = false,
                tm = DateTime.Now.ToString("yyyy-MM-dd'T'HH:mm:ss"),
                body=msg.body 
            }, msg.socket_id);
            if (idStatus != 0)
                return bdb.InsertMessage(msg.body, idStatus);
            else
                return new Models.RequestResult()
                {
                    ResultCode = 0,
                    ResultMessage = "16102020035:ошибка записи сообщения в базу"
                };
        }
        /// <summary>
        /// Ответ на контракт
        /// </summary>
        internal Models.RequestResult Response(response resp, int senderId)
        {
            int idStatus = 0;
            if (resp.isGuest)
                idStatus =  InsertStatus(resp.accept == 1 ? MessageTypes.acceptedGuest : MessageTypes.rejectedGuest, resp.bookId, senderId, resp.to, resp.refRealty, new ChatElement()
                {
                    idbook = resp.bookId,
                    refReciever = resp.to,
                    refSender = senderId,
                    messageType = resp.accept == 1 ? MessageTypes.acceptedGuest : MessageTypes.rejectedGuest,
                    mine = false,
                    tm = DateTime.Now.ToString("yyyy-MM-dd'T'HH:mm:ss")

                }, resp.socket_id);
            if (!resp.isGuest)
                idStatus =  InsertStatus(resp.accept == 1 ? MessageTypes.acceptedOwner : MessageTypes.rejectedOwner, resp.bookId, senderId, resp.to, resp.refRealty, new ChatElement()
                {
                    idbook = resp.bookId,
                    refReciever = resp.to,
                    refSender = senderId,
                    messageType = resp.accept == 1 ? MessageTypes.acceptedOwner : MessageTypes.rejectedOwner,
                    mine = false,
                    tm = DateTime.Now.ToString("yyyy-MM-dd'T'HH:mm:ss")

                }, resp.socket_id);
            if (idStatus != 0)
                return new Models.RequestResult()
                {
                    ResultCode = 1
                };
            else
                return new Models.RequestResult()
                {
                    ResultCode = 0,
                    ResultMessage = "16102020136:ошибка записи сообщения в базу"
                };
        } 
        internal ChatsList getChats(int asOwner, int userId)
        {
            return (new BookingMessageDB()).getChats(asOwner, userId);
        }
        /// <summary>
        /// Получение чата с пользователем по определеннной недвижимости, со статусами контрактов
        /// </summary> 
        /// <returns>Chat</returns
        internal Chat GetConversation(int contact, int refRealty, int myid)
        {
            return (new BookingMessageDB()).GetConversation(contact, refRealty, myid);
        }
         
    }
}