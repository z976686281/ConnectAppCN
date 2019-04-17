using System;
using System.Collections.Generic;
using ConnectApp.canvas;
using ConnectApp.components;
using ConnectApp.constants;
using ConnectApp.models;
using ConnectApp.Models.ActionModel;
using ConnectApp.plugins;
using ConnectApp.redux.actions;
using RSG;
using Unity.UIWidgets.painting;
using Unity.UIWidgets.rendering;
using Unity.UIWidgets.Redux;
using Unity.UIWidgets.widgets;

namespace ConnectApp.screens {
    internal static class LoginNavigatorRoutes {
        public const string Root = "/";
        public const string BindUnity = "/bind-unity";
    }

    public class LoginScreen : StatelessWidget {
        private static readonly GlobalKey globalKey = GlobalKey.key("login-router");
        public static NavigatorState navigator => globalKey.currentState as NavigatorState;

        private static Dictionary<string, WidgetBuilder> loginRoutes => new Dictionary<string, WidgetBuilder> {
            {LoginNavigatorRoutes.Root, context => new LoginSwitchScreenConnector()},
            {LoginNavigatorRoutes.BindUnity, context => new BindUnityScreenConnector(FromPage.login)}
        };

        public override Widget build(BuildContext context) {
            return new Navigator(
                globalKey,
                onGenerateRoute: settings => {
                    return new PageRouteBuilder(
                        settings,
                        (context1, animation, secondaryAnimation) => loginRoutes[settings.name](context1),
                        (context1, animation, secondaryAnimation, child) => new PushPageTransition(
                            routeAnimation: animation,
                            child: child
                        ));
                }
            );
        }
    }

    public class LoginSwitchScreenConnector : StatelessWidget {
        public override Widget build(BuildContext context) {
            return new StoreConnector<AppState, object>(
                converter: state => null,
                builder: (context1, _, dispatcher) => {
                    var actionModel = new LoginSwitchScreenActionModel {
                        mainRouterPop = () => dispatcher.dispatch(new MainNavigatorPopAction()),
                        loginByWechatAction = code => dispatcher.dispatch<IPromise>(Actions.loginByWechat(code)),
                        loginRouterPushToUnityBind= () => dispatcher.dispatch(new LoginNavigatorPushToBindUnityAction())
                    };
                    return new LoginSwitchScreen(actionModel);
                }
            );
        }
    }

    public class LoginSwitchScreen : StatelessWidget {
        public LoginSwitchScreen(
            LoginSwitchScreenActionModel actionModel
        ) {
            this.actionModel = actionModel;
        }

        private readonly LoginSwitchScreenActionModel actionModel;

        public override Widget build(BuildContext context) {
            return new Container(
                color: CColors.White,
                child: new SafeArea(
                    child: _buildContent(context)
                )
            );
        }

        private Widget _buildContent(BuildContext context) {
            return new Container(
                color: CColors.White,
                child: new Column(
                    children: new List<Widget> {
                        _buildTopView(),
                        _buildBottomView(context)
                    }
                )
            );
        }

        private Widget _buildTopView() {
            return new Flexible(
                child: new Stack(
                    children: new List<Widget> {
                        new Positioned(
                            top: 0,
                            left: 0,
                            child: new CustomButton(
                                padding: EdgeInsets.symmetric(10, 16),
                                onPressed: () => actionModel.mainRouterPop(),
                                child: new Icon(
                                    Icons.close,
                                    size: 24,
                                    color: CColors.icon3
                                )
                            )
                        ),
                        new Align(
                            alignment: Alignment.center,
                            child: new Container(
                                height: 144,
                                child: new Column(
                                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                                    children: new List<Widget> {
                                        new Container(
                                            child: new Icon(
                                                Icons.UnityLogo,
                                                size: 80
                                            )
                                        ),
                                        new Text(
                                            "欢迎来到 Unity Connect",
                                            maxLines: 1,
                                            style: CTextStyle.H4
                                        )
                                    }
                                )
                            ) 
                        )
                    }
                )
            );
        }

        private Widget _buildBottomView(BuildContext context) {
            return new Container(
                padding: EdgeInsets.symmetric(horizontal: 16),
                child: new Column(
                    children: new List<Widget> {
                        _buildWechatButton(context),
                        new Container(height: 16),
                        new CustomButton(
                            onPressed: () => actionModel.loginRouterPushToUnityBind(),
                            padding: EdgeInsets.zero,
                            child: new Container(
                                height: 48,
                                decoration: new BoxDecoration(
                                    CColors.White,
                                    border: Border.all(CColors.PrimaryBlue),
                                    borderRadius: BorderRadius.all(24)
                                ),
                                child: new Row(
                                    mainAxisAlignment: MainAxisAlignment.center,
                                    children: new List<Widget> {
                                        new Text(
                                            "使用 Unity ID 登录",
                                            maxLines: 1,
                                            style: CTextStyle.PLargeBlue
                                        )
                                    }
                                )
                            )
                        ),
                        new Container(
                            height: 65 + MediaQuery.of(context).padding.bottom
                        )
                    }
                )
            );
        }

        private Widget _buildWechatButton(BuildContext context) {
            if (!WechatPlugin.instance().inInstalled())
            {
                return new Container();
            }
            WechatPlugin.instance().context = context;
            return new CustomButton(
                onPressed: () => {
                    WechatPlugin.instance(code => {
                            CustomDialogUtils.showCustomDialog(
                                child: new CustomDialog()
                            );
                            actionModel.loginByWechatAction(code).Then(() => {
                                    CustomDialogUtils.hiddenCustomDialog();
                                    actionModel.mainRouterPop();
                                })
                                .Catch(_ => CustomDialogUtils.hiddenCustomDialog());
                        })
                        .login(Guid.NewGuid().ToString());
                },
                padding: EdgeInsets.zero,
                child: new Container(
                    height: 48,
                    decoration: new BoxDecoration(
                        CColors.PrimaryBlue,
                        borderRadius: BorderRadius.all(24)
                    ),
                    child: new Row(
                        mainAxisAlignment: MainAxisAlignment.center,
                        children: new List<Widget> {
                            new Icon(
                                Icons.WechatIcon,
                                size: 24,
                                color: CColors.White
                            ),
                            new Container(width: 8),
                            new Text(
                                "使用微信账号登陆",
                                maxLines: 1,
                                style: CTextStyle.PLargeWhite
                            )
                        }
                    )
                )
            );
        }
    }
}